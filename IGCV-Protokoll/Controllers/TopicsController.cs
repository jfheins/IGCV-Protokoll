﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Web.Mvc;
using EntityFramework.Extensions;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.util;
using IGCV_Protokoll.ViewModels;
using Newtonsoft.Json;

namespace IGCV_Protokoll.Controllers
{
	public class TopicsController : BaseController
	{
		private readonly string FQDN = "http://" + Dns.GetHostName() + ".igcv.fraunhofer.de";

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.TopicStyle = "active";
		}

		// GET: Topics
		/// <summary>
		///    Liefert eine tabellarische Auflistung der Themen. Der Benutzer kann nun die Themen filtern, oder auf die Detailseite
		///    wechseln. Das ViewModel <see cref="FilteredTopics" /> wird verwndet, um die Filterkriteriem zu transportieren.
		/// </summary>
		/// <param name="filter">Das ViewModel, dass die Filterkriterien angibt.</param>
		public ActionResult Index(FilteredTopics filter)
		{
			IQueryable<Topic> query = db.FilteredTopics(GetRolesForCurrentUser())
				.Include(t => t.SessionType)
				.Include(t => t.TargetSessionType)
				.Include(t => t.Creator)
				.Include(t => t.Votes)
				.Include(t => t.Tags);

			if (!filter.ShowReadonly)
				query = query.Where(t => !t.IsReadOnly);

			if (filter.ShowPriority >= 0)
				query = query.Where(t => t.Priority == (Priority)filter.ShowPriority);

			if (filter.SessionTypeID > 0)
				query = query.Where(t => t.SessionTypeID == filter.SessionTypeID || t.TargetSessionTypeID == filter.SessionTypeID);

			if (filter.Timespan != 0)
			{
				if (filter.Timespan > 0) // Nur die letzten x Tage anzeigen
				{
					var cutoff = DateTime.Today.AddDays(-filter.Timespan);
					query = query.Where(t => t.Created >= cutoff);
				}
				else // Alles VOR den letzten x Tagen anzeigen
				{
					var cutoff = DateTime.Today.AddDays(filter.Timespan);
					query = query.Where(t => t.Created < cutoff);
				}
			}

			if (filter.OwnerID != 0)
				query = query.Where(a => a.OwnerID == filter.OwnerID);

			if (filter.ShowTagsID.Count > 0)
			{
				var desired = filter.ShowTagsID.ToArray();
				query = query.Where(topic => topic.Tags.Any(tt => desired.Contains(tt.TagID)));
			}

			filter.UserList = CreateUserSelectList();
			filter.PriorityList = PriorityChoices(filter.ShowPriority);
			filter.SessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name");
			filter.TimespanList = TimespanChoices(filter.Timespan);
			filter.TagList = db.Tags.Select(tag => new SelectListItem
			{
				Text = tag.Name,
				// ReSharper disable once SpecifyACultureInStringConversionExplicitly
				Value = tag.ID.ToString()
			}).ToList();

			filter.Topics = query.OrderByDescending(t => t.Priority).ThenBy(t => t.Title).ToList();

			return View(filter);
		}

		internal static IEnumerable<SelectListItem> PriorityChoices(int preselect)
		{
			var placeholder = new SelectListItem
			{
				Text = "(Alle Prioritäten)",
				Value = "-1",
				Selected = preselect < 0
			}.ToEnumerable();

			var items = ((Priority[])Enum.GetValues(typeof(Priority)))
				.Select(p => new SelectListItem
				{
					Text = p.DisplayName(),
					Value = ((int)p).ToString(CultureInfo.InvariantCulture)
				});

			return placeholder.Concat(items);
		}

		/// <summary>
		///    Liefert die möglichen Zeitspannen, die ausgewählt werden können.
		/// </summary>
		/// <param name="preselect">Der Index, der vorselektiert wird.</param>
		/// <returns>Die Zeitspannen-Liste</returns>
		internal static IEnumerable<SelectListItem> TimespanChoices(int preselect)
		{
			return new[]
			{
				new SelectListItem
				{
					Text = "14 Tage",
					Value = "14",
					Selected = preselect == 14
				},
				new SelectListItem
				{
					Text = "30 Tage",
					Value = "30",
					Selected = preselect == 30
				},
				new SelectListItem
				{
					Text = "Älter als 30 Tage",
					Value = "-30",
					Selected = preselect == -30
				}
			};
		}

		// GET: Topics/Details/5
		/// <summary>
		///    Detailseite eines Themas
		/// </summary>
		/// <param name="id">Die TopicID, zu der die Details angezeigt werde soll.</param>
		public ActionResult Details(int? id)
		{
			if (id == null)
				return HTTPStatus(HttpStatusCode.BadRequest, "Für diesen Vorgang ist eine TopicID ist erforderlich.");

			var topic = db.Topics
				.Include(t => t.Assignments)
				.Include(t => t.Creator)
				.Include(t => t.Decision)
				.Include(t => t.Decision.Report)
				.Include(t => t.Lock)
				.Include(t => t.Lock.Session.Manager)
				.Include(t => t.Tags)
				.Include(t => t.Acl)
				.SingleOrDefault(t => t.ID == id.Value);

			if (topic == null)
				return HttpNotFound();

			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für dieses Thema nicht berechtigt!");

			// Ungelesen-Markierung aktualisieren
			MarkAsRead(topic);

			ViewBag.TopicID = id.Value;
			ViewBag.TopicHistoryCount = db.TopicHistory.Count(t => t.TopicID == id.Value);
			ViewBag.IsEditable = topic.IsEditableBy(GetCurrentUser(), GetSession()).IsAuthorized;
			ViewBag.TagDict = CreateTagDictionary(topic.Tags);
			ViewBag.Host = FQDN;
			ViewBag.IsTopicOnDashboard = HomeController.IsTopicOnDashboard(db, GetCurrentUserID(), topic);

			topic.IsLocked = IsTopicLocked(id.Value);
			return View(topic);
		}

		//AJAX: Topics/AddTag/5?tagid=3
		/// <summary>
		///    Fügt ein Tag zu einem Thema hinzu.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		/// <param name="tagid">Die TagID</param>
		[HttpPost]
		public ActionResult AddTag(int id, int tagid)
		{
			var relation = new TagTopic { TopicID = id, TagID = tagid };
			db.TagTopics.AddOrUpdate(t => new { t.TopicID, t.TagID }, relation);
			try
			{
				db.SaveChanges();
			}
			catch (DbEntityValidationException e)
			{
				return HTTPStatus(HttpStatusCode.InternalServerError, ErrorMessageFromException(e));
			}
			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}

		//AJAX: Topics/RemoveTag/5?tagid=3
		/// <summary>
		///    Entfernt ein Tag von einem Thema.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		/// <param name="tagid">Die TagID</param>
		[HttpPost]
		public ActionResult RemoveTag(int id, int tagid)
		{
			db.TagTopics.Where(tt => tt.TopicID == id && tt.TagID == tagid).Delete();
			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}

		private void PopulateViewModelWithPresets(TopicCreateVM vm)
		{
			var presets = new List<AclPreset>
			{
				new AclPreset { ID = 0, Name = "Standard", EntityList = GetCurrentUser().Settings.AclTreePreset },
			};
			var sessionTypeAttendees = db.SessionTypes.Where(st => st.Active)
				.Select(st => new { st, adIDs = st.Attendees.Select(u => u.AdGroups.FirstOrDefault(g => g.AdEntity.Type == AdEntityType.User)) });

			var mapSessionTypeToEntities = sessionTypeAttendees.ToDictionary(x => x.st.ID, x => x.adIDs.WhereNotNull().Select(y => y.AdEntityID).ToArray());

			vm.SessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name");
			vm.TargetSessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name");
			vm.UserList = CreateUserSelectList();
			vm.AvailableAclPresets = presets;
			vm.MapSTtoAdEntities = mapSessionTypeToEntities;
		}

		// GET: Topics/Create
		/// <summary>
		///    Ruft das Formular zum Erstellen eines neuen Themas ab.
		/// </summary>
		public ActionResult Create()
		{
			var viewmodel = new TopicCreateVM();
			PopulateViewModelWithPresets(viewmodel);
			return View(viewmodel);
		}

		// POST: Topics/Create
		/// <summary>
		///    Erstellt ein neues Thema auf Basis der angegebenen Werte. Das ViewModel <see cref="TopicEdit" /> wird verwendet.
		/// </summary>
		/// <param name="input">Der Inhalt des neuen Themas.</param>
		/// <param name="aclTreeJson">ACL-Baum im JSON Format</param>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create([Bind(Exclude = "TargetSessionTypeID")] TopicCreateVM input, string aclTreeJson)
		{
			if (input.TopicType == TopicType.Discussion && input.Proposal == string.Empty)
				ModelState.AddModelError("Proposal", "Das Feld \"Beschlussvorschlag\" ist bei einer Diskussion erforderlich.");

			if (ModelState.IsValid)
			{
				var t = new Topic
				{
					CreatorID = GetCurrentUserID(),
					SessionTypeID = input.SessionTypeID,
					
				};
				t.IncorporateUpdates(input);
				t.DocumentContainer.Add(new DocumentContainer());

				// ACL
				var selectedAclTree = JsonConvert.DeserializeObject<List<SelectedAdEntity>>(aclTreeJson);
				// Wenn alles selektiert ist, kann auf eine ACL verzichtet werden. Standardrechte sind ja "Jeder".
				if (!selectedAclTree.All(x => x.selected))
					ApplyNewACLFor(t, selectedAclTree.Where(x => x.selected).Select(x => x.id));

				foreach (User user in db.SessionTypes
					.Include(st => st.Attendees)
					.Single(st => st.ID == input.SessionTypeID).Attendees)
				{
					t.Votes.Add(new Vote(user, VoteKind.None));
				}

				db.Topics.Add(t);

				// Falls in einer Sitzung ein neues Thema erzeugt wird, kann dieses der Sitzung zugeschlagen werden.
				var session = GetSession();
				if (session != null && session.SessionTypeID == input.SessionTypeID)
				{
					session.LockedTopics.Add(new TopicLock
					{
						Topic = t,
						Session = session
					});
				}

				// Ungelesen-Markierung aktualisieren
				MarkAsUnread(t);

				try
				{
					db.SaveChanges();
				}
				catch (DbEntityValidationException e)
				{
					var message = ErrorMessageFromException(e);
					return HTTPStatus(HttpStatusCode.InternalServerError, message);
				}

				return RedirectToAction("Details", new { id = t.ID });
			}

			PopulateViewModelWithPresets(input);
			
			input.SessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name", input.SessionTypeID);
			input.TargetSessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name", input.TargetSessionTypeID);

			return View(input);
		}

		// GET: Topics/Edit/5
		/// <summary>
		///    Ruft das Formular zum Bearbeiten eines Themas ab. Das ViewModel <see cref="TopicEdit" /> wird verwendet.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		/// <exception cref="TopicLockedException">Wird geworfen, falls das Thema gesperrt ist.</exception>
		public ActionResult Edit(int? id)
		{
			if (id == null)
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

			Topic topic = db.Topics.Find(id);

			if (topic == null)
				return HttpNotFound();
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			var auth = topic.IsEditableBy(GetCurrentUser(), GetSession());
			if (!auth.IsAuthorized)
				throw new TopicLockedException(auth.Reason);

			TopicEdit viewmodel = TopicEdit.FromTopic(topic);
			viewmodel.SessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name");
			viewmodel.TargetSessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name");
			viewmodel.UserList = CreateUserSelectList(viewmodel.OwnerID);

			return View(viewmodel);
		}

		// POST: Topics/Edit/5
		/// <summary>
		///    Verarbeitet die Bearbeitugn eines Themas. Die Ursprungsversion wird gesichert, bevor die vorgenommenen Änderungen
		///    gespeichert werden. Das ViewModel <see cref="TopicEdit" /> wird verwendet.
		/// </summary>
		/// <param name="input">Die Änderungen</param>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit([Bind(Exclude = "SessionTypeID")] TopicEdit input)
		{
			Topic topic = db.Topics.Include(t => t.Creator).Single(t => t.ID == input.ID);
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");
			
			if (input.TopicType == TopicType.Discussion && input.Proposal == string.Empty)
				ModelState.AddModelError("Proposal", "Das Feld \"Beschlussvorschlag\" ist bei einer Diskussion erforderlich.");

			if (ModelState.IsValid)
			{
				var auth = topic.IsEditableBy(GetCurrentUser(), GetSession());
				if (!auth.IsAuthorized)
					return HTTPStatus(HttpStatusCode.Forbidden, auth.Reason);

				if (input.Proposal != topic.Proposal)
				{
					// Falls der Beschlussvorschlag geändert wurde, werden die Stimmen zurückgesetzt.
					foreach (var vote in topic.Votes)
						vote.Kind = VoteKind.None;
				}

				// Änderungsverfolgung
				db.TopicHistory.Add(TopicHistory.FromTopic(topic, GetCurrentUserID()));
				topic.IncorporateUpdates(input);

				// Irrelevante Verschiebung auflösen
				topic.TargetSessionTypeID = input.TargetSessionTypeID == topic.SessionTypeID ? null : input.TargetSessionTypeID;

				// Ggf. neue Stimmberechtigte hinzufügen
				if (topic.TargetSessionTypeID > 0)
				{
					var voters = new HashSet<int>(topic.Votes.Select(v => v.Voter.ID));

					foreach (User user in db.SessionTypes
						.Include(st => st.Attendees)
						.Single(st => st.ID == input.TargetSessionTypeID)
						.Attendees.Where(user => !voters.Contains(user.ID)))
					{
						topic.Votes.Add(new Vote(user, VoteKind.None));
					}

					if (topic.Lock != null)
						topic.Lock.Action = TopicAction.None;
				}

				// Ungelesen-Markierung aktualisieren
				MarkAsUnread(topic);

				try
				{
					db.SaveChanges();
				}
				catch (DbEntityValidationException e)
				{
					var message = ErrorMessageFromException(e);
					return HTTPStatus(HttpStatusCode.InternalServerError, message);
				}

				return RedirectToAction("Details", new { Area = "", id = input.ID });
			}
			input.SessionType = topic.SessionType;
			input.SessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name", input.SessionTypeID);
			input.TargetSessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name", input.TargetSessionTypeID);
			input.UserList = CreateUserSelectList();
			return View(input);
		}

		// AJAX: Topics/_EditDescription/5
		/// <summary>
		///    Ermöglicht das "schnelle Bearbeiten" der Themenbeschreibung aus der Detailansicht. Das Formular wird abgerufen.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		/// <returns>Formular zum schnellen Bearbeiten</returns>
		[HttpGet, ActionName("_EditDescription")]
		public ActionResult _BeginEditDescription(int id)
		{
			var topic = db.Topics.Find(id);

			if (topic == null)
				return HttpNotFound("Topic not found!");
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			ViewBag.TopicID = id;
			ViewBag.IsEditable = topic.IsEditableBy(GetCurrentUser(), GetSession()).IsAuthorized;
			topic.IsLocked = false;

			return PartialView("_EditDescription", topic);
		}

		// AJAX: Topics/_EditDescription/5
		/// <summary>
		///    Ermöglicht das "schnelle Bearbeiten" der Themenbeschreibung aus der Detailansicht. Die Änderungen werden
		///    gespeichert.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		/// <param name="description">Die neue Beschreibung</param>
		[HttpPost, ActionName("_EditDescription"), ValidateAntiForgeryToken]
		public ActionResult _SubmitEditDescription(int id, string description)
		{
			var topic = db.Topics.Find(id);

			if (topic == null)
				return HttpNotFound("Topic not found!");
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			var auth = topic.IsEditableBy(GetCurrentUser(), GetSession());
			if (!auth.IsAuthorized)
				return HTTPStatus(HttpStatusCode.InternalServerError, auth.Reason);

			if (description != topic.Description) //Trivialedit verhindern
			{
				// Änderungsverfolgung
				db.TopicHistory.Add(TopicHistory.FromTopic(topic, GetCurrentUserID()));
				topic.Description = description;
				topic.ValidFrom = DateTime.Now;

				// Ungelesen-Markierung aktualisieren
				MarkAsUnread(topic);

				try
				{
					db.SaveChanges();
				}
				catch (DbEntityValidationException e)
				{
					var message = ErrorMessageFromException(e);
					return HTTPStatus(HttpStatusCode.InternalServerError, message);
				}
			}

			ViewBag.TopicID = id;
			ViewBag.IsEditable = topic.IsEditableBy(GetCurrentUser(), GetSession()).IsAuthorized;
			topic.IsLocked = false;
			return PartialView("_Description", topic);
		}

		// AJAX: Topics/_FetchDescription/5
		/// <summary>
		///    Ermöglicht das "schnelle Bearbeiten" der Themenbeschreibung aus der Detailansicht. Die Methode liefert die aktuelle
		///    Beschreibung zurück.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		public ActionResult _FetchDescription(int id)
		{
			var topic = db.Topics.Find(id);

			if (topic == null)
				return HttpNotFound("Topic not found!");
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			ViewBag.TopicID = id;
			ViewBag.IsEditable = topic.IsEditableBy(GetCurrentUser(), GetSession()).IsAuthorized;
			topic.IsLocked = false;

			return PartialView("_Description", topic);
		}

		// AJAX: Topics/_EditProposal/5
		/// <summary>
		///    Ermöglicht das "schnelle Bearbeiten" des Beschlussvorschlags aus der Detailansicht. Das Formular wird abgerufen.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		/// <returns>Formular zum schnellen Bearbeiten</returns>
		[HttpGet, ActionName("_EditProposal")]
		public ActionResult _BeginEditProposal(int id)
		{
			var topic = db.Topics.Find(id);

			if (topic == null)
				return HttpNotFound("Topic not found!");
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			ViewBag.TopicID = id;
			ViewBag.IsEditable = topic.IsEditableBy(GetCurrentUser(), GetSession()).IsAuthorized;
			topic.IsLocked = false;

			return PartialView("_EditProposal", topic);
		}

		// AJAX: Topics/_SubmitEditDescription/5
		/// <summary>
		///    Ermöglicht das "schnelle Bearbeiten" des Beschlussvorschlags aus der Detailansicht. Die Änderungen werden
		///    gespeichert.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		/// <param name="proposal">Der neue Beschlussvorschlag</param>
		[HttpPost, ActionName("_EditProposal"), ValidateAntiForgeryToken]
		public ActionResult _SubmitEditProposal(int id, string proposal)
		{
			var topic = db.Topics.Find(id);

			if (topic == null)
				return HttpNotFound("Topic not found!");
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			var auth = topic.IsEditableBy(GetCurrentUser(), GetSession());
			if (!auth.IsAuthorized)
				return HTTPStatus(HttpStatusCode.InternalServerError, auth.Reason);

			if (proposal != topic.Proposal || topic.TopicType == TopicType.Report) //Trivialedit verhindern
			{
				// Änderungsverfolgung
				db.TopicHistory.Add(TopicHistory.FromTopic(topic, GetCurrentUserID()));
				topic.Proposal = proposal;
				topic.ValidFrom = DateTime.Now;
				topic.TopicType = TopicType.Discussion;

				// Ungelesen-Markierung aktualisieren
				MarkAsUnread(topic);
				foreach (var vote in topic.Votes)
				{
					vote.Kind = VoteKind.None;
				}

				try
				{
					db.SaveChanges();
				}
				catch (DbEntityValidationException e)
				{
					var message = ErrorMessageFromException(e);
					return HTTPStatus(HttpStatusCode.InternalServerError, message);
				}
			}

			ViewBag.TopicID = id;
			ViewBag.IsEditable = topic.IsEditableBy(GetCurrentUser(), GetSession()).IsAuthorized;
			topic.IsLocked = false;
			return PartialView("_Proposal", topic);
		}

		// AJAX: Topics/_FetchDescription/5
		/// <summary>
		///    Ermöglicht das "schnelle Bearbeiten" des Beschlussvorschlags aus der Detailansicht. Die Methode liefert dien
		///    aktuellen Beschlussvorschlag zurück.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		public ActionResult _FetchProposal(int id)
		{
			var topic = db.Topics.Find(id);

			if (topic == null)
				return HttpNotFound("Topic not found!");
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			ViewBag.TopicID = id;
			ViewBag.IsEditable = topic.IsEditableBy(GetCurrentUser(), GetSession()).IsAuthorized;
			topic.IsLocked = false;

			return PartialView("_Proposal", topic);
		}

		// GET: Topics/ViewHistory/5
		/// <summary>
		/// Zeigt die Änderungen, die an dem Thema vorgenommen wurden.
		/// </summary>
		/// <param name="id">Die TopicID</param>
		public ActionResult ViewHistory(int id)
		{
			Topic topic = db.Topics.Find(id);

			if (topic == null)
				return HttpNotFound("Das Thema konnte nicht gefunden werden.");
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			var history = db.TopicHistory.Where(th => th.TopicID == topic.ID).OrderBy(th => th.ValidFrom).ToList();

			if (history.Count == 0)
				return RedirectToAction("Details", new { id });

			history.Add(TopicHistory.FromTopic(topic, 0));

			var vm = new TopicHistoryViewModel
			{
				Usernames = db.Users.ToDictionary(u => u.ID, u => u.ShortName),
				SessionTypes = db.SessionTypes.ToDictionary(s => s.ID, s => s.Name),
				Current = topic,
				Initial = history[0]
			};

			var diff = new diff_match_patch { Diff_Timeout = 0.4f };

			// Falls in der Zwischenzeit Sitzungstypen gelöscht wurden, sollten die IDs für die GUI ersatzweise hinterlegt werden.
			var neededSessionIds = history.Select(th => th.SessionTypeID);
			neededSessionIds = neededSessionIds.Concat(history.Where(h => h.TargetSessionTypeID != null).Select(h => h.TargetSessionTypeID.Value));
			foreach (var stid in neededSessionIds.Except(vm.SessionTypes.Keys).ToArray())
			{
				vm.SessionTypes.Add(stid, "<Gelöscht>");
			}

			// Falls in der Zwischenzeit Benutzer gelöscht wurden, ohne die IDs gerade zu ziehen, IDs ersatzweise hinterlegen.
			var neededUserIds = history.Select(th => th.OwnerID).Concat(history.Select(h => h.EditorID));
			foreach (var userid in neededUserIds.Except(vm.Usernames.Keys).ToArray())
			{
				vm.Usernames.Add(userid, "<Gelöscht>");
			}

			// Anonyme Funktion, um die Bibliothek diff_match_patch handlich zu verpacken.
			Func<string, string, List<Diff>> textDiff = (a, b) =>
			{
				var list = diff.diff_main(a, b);
				diff.diff_cleanupSemantic(list);
				return list;
			};

			foreach (var p in history.Pairwise())
			{
				vm.Differences.Add(new TopicHistoryDiff
				{
					// Ein Eintrag entspricht später einer Box auf der Seite. Wenn keine Änderung existiert, sollte hier null gespeichert werden. Bei einer Änderung wird der NEUE Wert (der in Item2 enthalten ist) genommen.
					// SimpleDiff ist eine kleine Helferfunktion, da die Zeilen sonst arg lang werden würden. Hier wird kein Text vergleichen - entweder hat sich alles geändert, oder gar nichts. (Daher "simple")
					// textDiff ist komplexer, hier wird der Text analysiert und auf ähnliche Abschnitte hin untersucht.
					Modified = p.Item1.ValidUntil,
					Editor = vm.Usernames[p.Item1.EditorID],
					SessionType = SimpleDiff(p.Item1.SessionTypeID, p.Item2.SessionTypeID, vm.SessionTypes),
					TargetSessionType = SimpleDiff(p.Item1.TargetSessionTypeID, p.Item2.TargetSessionTypeID, vm.SessionTypes, "(kein)"),
					Owner = SimpleDiff(p.Item1.OwnerID, p.Item2.OwnerID, vm.Usernames),
					Priority = p.Item1.Priority == p.Item2.Priority ? null : p.Item2.Priority.DisplayName(),
					Title = textDiff(p.Item1.Title, p.Item2.Title),
					Time = p.Item1.Time == p.Item2.Time ? null : p.Item2.Time,
					Description = textDiff(p.Item1.Description, p.Item2.Description),
					Proposal = textDiff(p.Item1.Proposal, p.Item2.Proposal)
				});
			}

			return View(vm);
		}

		/// <summary>
		///    Vergleicht die beiden IDs und gibt den Unterschied zurück. Bei Gleichheit wird null zurückgegeben.
		///    Bei verschiedenen Ids wird der Rückgabewert aus dem Dictionary anhand von idB ermittelt.
		/// </summary>
		/// <param name="idA">Erste ID</param>
		/// <param name="idB">Zweite ID</param>
		/// <param name="dict">Lookup für den Rückgabewert</param>
		/// <returns>null, wenn idA == idB, dict[idB] sonst.</returns>
		private static string SimpleDiff(int idA, int idB, IDictionary<int, string> dict)
		{
			return idA == idB ? null : dict.GetValueOrDefault(idB, "<Nicht verfügbar>");
		}

		/// <summary>
		///    Vergleicht die beiden IDs und gibt den Unterschied zurück. Bei Gleichheit wird null zurückgegeben.
		///    Bei verschiedenen Ids wird der Rückgabewert aus dem Dictionary anhand von idB ermittelt.
		/// </summary>
		/// <param name="idA">Erste ID</param>
		/// <param name="idB">Zweite ID</param>
		/// <param name="dict">Lookup für den Rückgabewert</param>
		/// <param name="defaultText">Standardtext für den Rückgabewert, wenn idB null ist</param>
		/// <returns>null, wenn idA == idB, dict[idB] wenn idB != null, defaultText sonst.</returns>
		private static string SimpleDiff(int? idA, int? idB, IDictionary<int, string> dict, string defaultText)
		{
			return idA == idB ? null : (idB.HasValue ? dict.GetValueOrDefault(idB.Value, "<Nicht verfügbar>") : defaultText);
		}

		[HttpPost]
		public ActionResult _SaveAclFor(int topicID, string aclTree)
		{
			var topic = db.Topics
						.Include(t => t.Acl)
						.SingleOrDefault(t => t.ID == topicID);

			var result = SaveAclFor(topic, aclTree);

			return result ?? PartialView("_AclDisplay", topic);
		}

		[HttpPost]
		public ActionResult DisplayOnDashboard(int id)
		{
			var displayOverride = new TopicVisibilityOverride { TopicID = id, UserID = GetCurrentUserID(), Visibility = VisibilityOverride.Show };
			db.TopicVisibilityOverrides.AddOrUpdate(x => new { x.TopicID, x.UserID }, displayOverride);
			db.SaveChanges();
			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}

		[HttpPost]
		public ActionResult HideFromDashboard(int id)
		{
			var displayOverride = new TopicVisibilityOverride { TopicID = id, UserID = GetCurrentUserID(), Visibility = VisibilityOverride.Hide };
			db.TopicVisibilityOverrides.AddOrUpdate(x => new { x.TopicID, x.UserID }, displayOverride);
			db.SaveChanges();
			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}

		public ActionResult Clone(int id)
		{
			var old = db.Topics.Include(t => t.Comments).Include(t => t.Votes).Include(t => t.DocumentContainer).SingleOrDefault(t => t.ID == id);
			if (old == null)
				return HttpNotFound("Topic not found!");
			if (!IsAuthorizedFor(old))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			var newTopic = db.Topics.Add(new Topic
			{
				OwnerID = GetCurrentUserID(),
				SessionTypeID = old.SessionTypeID,
				Title = old.Title + " (Klon)",
				Time = old.Time,
				Description = old.Description,
				Proposal = old.Proposal,
				Priority = old.Priority,
				CreatorID = GetCurrentUserID(),
				IsReadOnly = false,
				TopicType = old.TopicType
			});
			newTopic.DocumentContainer.Add(db.DocumentContainers.Add(new DocumentContainer()));

			newTopic.Votes = new List<Vote>(old.Votes.Select(v => new Vote { Kind = v.Kind, VoterID = v.VoterID, Topic = newTopic }));
			newTopic.PushTargets = new List<PushNotification>(old.PushTargets.Select(p => new PushNotification { UserID = p.UserID }));
			newTopic.Assignments = new List<Assignment>(old.Assignments.Select(a => new Assignment
			{
				Description = a.Description,
				DueDate = a.DueDate,
				IsActive = false,
				OwnerID = a.OwnerID,
				IsDone = a.IsDone,
				ReminderSent = false,
				Title = a.Title,
				Type = a.Type
			}));
			newTopic.Tags = new List<TagTopic>(old.Tags.Select(tt => new TagTopic { TagID = tt.TagID, Topic = newTopic }));

			newTopic.LeftLinks = new List<TopicLink>(old.LeftLinks.Select(l => new TopicLink { RightTopicID = l.RightTopicID, LinkTemplateID = l.LinkTemplateID }));
			newTopic.RightLinks = new List<TopicLink>(old.RightLinks.Select(l => new TopicLink { LeftTopicID = l.LeftTopicID, LinkTemplateID = l.LinkTemplateID }));

			// ACL kopieren
			if (old.Acl != null)
			{
				newTopic.Acl = db.ACLs.Add(new ACL());
				newTopic.Acl.Items = new List<ACLItem>(old.Acl.Items.Select(i => new ACLItem { AdEntityID = i.AdEntityID }));
			}

			db.SaveChanges();

			// Inhalt des Dokumentencontainers kopieren
			foreach (var oldDoc in old.Documents.VisibleDocuments)
			{
				newTopic.Documents.Documents.Add(CreateCopyOf(oldDoc));
			}

			db.SaveChanges();
			foreach (var document in newTopic.Documents.Documents)
			{
				document.LatestRevision = document.Revisions.OrderByDescending(r => r.Created).First();
			}
			db.SaveChanges();

			return RedirectToAction("Details", new { id = newTopic.ID });
		}

		private Document CreateCopyOf(Document oldDoc)
		{
			var doc = db.Documents.Add(new Document
			{
				Created = oldDoc.Created,
				DisplayName = oldDoc.DisplayName,
				Revisions = new List<Revision>(oldDoc.Revisions.Select(CreateCopyOf)),
				GUID = Guid.NewGuid(),
				LatestRevisionID = null
			});
			return doc;
		}

		private Revision CreateCopyOf(Revision oldRev)
		{
			return db.Revisions.Add(new Revision
			{
				Created = oldRev.Created,
				Extension = oldRev.Extension,
				FileSize = oldRev.FileSize,
				SafeName = oldRev.SafeName,
				UploaderID = oldRev.UploaderID,
				GUID = Guid.NewGuid(),
				DiskName = oldRev.FileName
			});
		}
	}
}