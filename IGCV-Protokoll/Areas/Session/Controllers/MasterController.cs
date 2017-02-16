using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Administration.Models;
using IGCV_Protokoll.Areas.Session.Models;
using IGCV_Protokoll.Models;
using JetBrains.Annotations;

namespace IGCV_Protokoll.Areas.Session.Controllers
{
	public class MasterController : SessionBaseController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.SMasterStyle = "active";
		}

		// GET: Session/Master
		public ActionResult Index()
		{
			var session = GetSession();
			if (session != null)
				return RedirectToAction("Edit");

			var thisUserID = GetCurrentUserID();
			var availableSessions = GetActiveSessionTypes().ToList();

			var runningSessions = db.ActiveSessions.Include(s => s.Manager)
				.Where(s => s.SessionType.Attendees.Any(a => a.ID == thisUserID)).ToList();

			ViewBag.ErrorMessage = TempData["ErrorMessage"];
			ViewBag.SessionTypes = new SelectList(availableSessions, "ID", "Name");
			return View(runningSessions);
		}

		public ActionResult Create(int SessionTypeID)
		{
			var uid = GetCurrentUserID();
			var activeSession = (from s in db.ActiveSessions
								 where s.SessionType.ID == SessionTypeID
								 select new { s.ID, s.ManagerID }).SingleOrDefault();

			if (activeSession != null)
			{
				if (activeSession.ManagerID == uid)
					return View(ResumeSession(activeSession.ID));

				TempData["ErrorMessage"] = "Es läuft bereits eine Sitzung dieses Typs. Benutzen Sie die Aktion 'Übernehmen' um die Sitzungsleitung zu übernehmen.";
				return RedirectToAction("Index");
			}

			var st = db.SessionTypes.Find(SessionTypeID);
			if (st == null)
				return HttpNotFound("Sitzungstyp nicht gefunden.");
			//if (st.Attendees.All(u => u.ID != uid))
			//	return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind kein Stammteilnehmer. Nur Stammteilnehmer können die Sitzung eröffnen.");

			return View(CreateNewSession(st));
		}

		public ActionResult Resume(int SessionID)
		{
			try
			{
				ResumeSession(SessionID);
			}
			catch (ArgumentException)
			{
				return new HttpStatusCodeResult(HttpStatusCode.NotFound, "Sitzungstyp nicht gefunden.");
			}
			return RedirectToAction("Edit");
		}

		[HttpGet]
		public ActionResult Edit()
		{
			var session = GetSession();
			if (session == null)
				return RedirectToAction("Index");

			ViewBag.UserDict = session.SessionType.Attendees.ToDictionary(u => u, u => session.PresentUsers.Contains(u));

			return View(session);
		}

		[HttpPost]
		public ActionResult Edit([Bind(Prefix = "Users")] Dictionary<int, bool> selectedUsers,
			[Bind(Include = "AdditionalAttendees,Notes")] ActiveSession input)
		{
			var session = GetSession();
			if (session == null)
				return RedirectToAction("Index");

			session.AdditionalAttendees = input.AdditionalAttendees;
			session.Notes = input.Notes;

			foreach (var kvp in selectedUsers)
			{
				if (kvp.Value)
					session.PresentUsers.Add(db.Users.Find(kvp.Key));
				else
					session.PresentUsers.Remove(db.Users.Find(kvp.Key));
			}

			db.SaveChanges();

			return RedirectToAction("Index", "Lists", new { Area = "Session" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult AbortSession(int id)
		{
			var session = GetSession();
			if (session == null)
				return RedirectToAction("Index");

			if (session.ID != id)
				return HTTPStatus(422, "Die Sitzungs-ID stimmt nicht überein!");

			session.PresentUsers.Clear();
			db.ActiveSessions.Remove(session);
			try
			{
				db.SaveChanges();
			}
			catch (DbEntityValidationException e)
			{
				var message = ErrorMessageFromException(e);
				return HTTPStatus(HttpStatusCode.InternalServerError, message);
			}
			Session.Remove("SessionID");

			return RedirectToAction("Index", "Master", new { Area = "Session" });
		}

		private ActiveSession CreateNewSession([NotNull] SessionType type)
		{
			var session = db.ActiveSessions.Add(new ActiveSession(type)
			{
				ManagerID = GetCurrentUserID()
			});

			// Agenda aus Template instanziieren und damit festlegen
			session.ActiveAgendaItems = type.Agenda?.AgendaItems.Select(ai => ActiveAgendaItem.FromTemplate(ai, session)).ToList();

			// GGf. Themen übernehmen, die in die aktuelle Sitzung hinein verschoben werden.
			foreach (var t in db.Topics.Include(t => t.Creator).Where(t => t.TargetSessionTypeID == type.ID))
			{
				t.SessionTypeID = type.ID;
				t.TargetSessionTypeID = null;
			}
			try
			{
				db.SaveChanges();
			}
			catch (DbEntityValidationException e)
			{
				throw new InvalidOperationException(ErrorMessageFromException(e), e);
			}

			var topics = db.Topics
				.Where(t => t.Decision == null && !t.IsReadOnly)
				.Where(t => t.SessionTypeID == session.SessionType.ID && t.TargetSessionTypeID == null)
				.Where(t => t.ResubmissionDate == null || t.ResubmissionDate < DateTime.Now)
				.ToList();

			foreach (var topic in topics)
			{
				MarkAsUnread(topic, skipCurrentUser: false);

				AuthorizeCurrentUserFor(topic);
				session.LockedTopics.Add(new TopicLock
				{
					Topic = topic,
					Session = session,
					Action = TopicAction.None
				});
			}

			db.SaveChanges();
			Session["SessionID"] = session.ID;
			return session;
		}

		private ActiveSession ResumeSession(int sessionID)
		{
			var session = db.ActiveSessions.Find(sessionID);

			if (session == null)
				throw new ArgumentException("Session-ID was not found.");

			Session["SessionID"] = session.ID;
			session.ManagerID = GetCurrentUserID();

			foreach (var tlock in session.LockedTopics)
			{
				MarkAsUnread(tlock.Topic, skipCurrentUser: false);
			}
			db.SaveChanges();
			return session;
		}
	}
}