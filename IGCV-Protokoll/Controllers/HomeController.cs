using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using IGCV_Protokoll.DataLayer;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.ViewModels;
using Microsoft.Ajax.Utilities;
using StackExchange.Profiling;

namespace IGCV_Protokoll.Controllers
{
	/// <summary>
	///    Dies ist die Startseite, deren Index()-methode auch ohne Angabe des Controllers ausgelöst werden kann. ~/ verweist
	///    also auf diese Index()-Methode.
	/// </summary>
	public class HomeController : BaseController
	{
		// GET: Home/Index/
		/// <summary>
		///    Hier wird das Dashboard generiert. Das Dashboard ist die Startseite des IGCV-Protokolls und der Ausgangspunkt für
		///    alle weiteren Aktionenn des Nutzers. Es wird das ViewModel <see cref="DashBoard" /> genutzt, um die Daten zu
		///    verpacken. Wird verzögertes Laden eingesetzt, muss die Property <see cref="DashBoard.MyTopics" /> auf null gesetzt
		///    werden. Die View generiert dann Code, der die Themen nachträglich von der Methode <see cref="_FetchTopics" />
		///    anfordert.
		/// </summary>
		public ActionResult Index()
		{
			var userID = GetCurrentUserID();
			var dash = new DashBoard();
			var profiler = MiniProfiler.Current;

			using (profiler.Step("Push-Nachrichten"))
			{
				dash.Notifications =
					db.PushNotifications.Include(pn => pn.Topic)
						.Where(pn => pn.UserID == userID && pn.Topic.IsReadOnly && !pn.Confirmed)
						.ToList();
			}
			using (profiler.Step("Aufgaben"))
			{
				var myAssignments = db.Assignments.Where(a => a.Owner.ID == userID && !a.IsDone && a.IsActive)
					.ToLookup(a => a.Type);
				dash.MyToDos = myAssignments[AssignmentType.ToDo];
				dash.MyDuties = myAssignments[AssignmentType.Duty].Where(a => a.Topic.HasDecision(DecisionType.Resolution));
			}

			dash.MyTopics = null; // Delayed AJAX loading
			return View(dash);
		}

		// AJAX: Home/_FetchTopics/
		/// <summary>
		///    Die Auflistung aller Themen des Dashboard wird generiert. Ausgehend von dieser kann der Benutzer direkt abstimmen,
		///    Kommentare schreiben oder Anhänge aufrufen.
		/// </summary>
		public ActionResult _FetchTopics()
		{
			var userID = GetCurrentUserID();
			var viewModel = new CompleteTopicList();

			using (MiniProfiler.Current.Step("Themen"))
			{
				// Die folgenden Includes erzeugen fast alle einen LEFT JOIN. Includes diesen grundsätzlich dazu, weniger Datenbankabfragen abzusetzen und dadurch Zeit zu sparen.
				// Durch jeden LEFT JOIN potenziert sich allerdings die übertragene Datenmenge und die Zeit, die EF braucht, erhöht sich entsprechend.
				var cutoff = DateTime.Now.AddDays(3);
				viewModel.Topics = db.FilteredTopics(GetRolesForCurrentUser())
					.Include(t => t.Owner)
					.Include(t => t.SessionType)
					.Include(t => t.TargetSessionType)
					.Include(t => t.Votes)
					.Where(t => !t.IsReadOnly)
					.Where(t => t.ResubmissionDate == null || t.ResubmissionDate < cutoff)
					.Where(t => t.VisibilityOverrides.Any(x => x.UserID == userID) ? 
							t.VisibilityOverrides.FirstOrDefault(x => x.UserID == userID).Visibility == VisibilityOverride.Show :
							(t.OwnerID == userID || t.Votes.Any(v => v.Voter.ID == userID))
					)
					.OrderByDescending(t => t.Priority)
					.ThenByDescending(t => t.ValidFrom).ToList();
				
			}

			var topicids = viewModel.Topics.Select(t => t.ID).Distinct().ToArray();


			using (MiniProfiler.Current.Step("Unread, Tags, Locks"))
			{
				viewModel.UnreadTopics = new HashSet<int>(from u in db.UnreadState where u.UserID == userID && topicids.Contains(u.TopicID) select u.TopicID);
				viewModel.Tags = (from t in db.TagTopics where topicids.Contains(t.TopicID) select t).ToLookup(t => t.TopicID);
				viewModel.LockedTopics = new HashSet<int>(from tl in db.TopicLocks where topicids.Contains(tl.TopicID) && tl.Session.ManagerID != userID select tl.TopicID);
			}

			using (MiniProfiler.Current.Step("Kommentare, Dokumente"))
			{
				viewModel.Comments =
					db.Comments.Include(c => c.Author).Where(c => topicids.Contains(c.TopicID)).ToLookup(c => c.TopicID);

				viewModel.Documents = db.DocumentContainers.Include(dc => dc.Documents)
						.Where(dc => dc.TopicID.HasValue)
						.Where(dc => topicids.Contains(dc.TopicID.Value))
					// ReSharper disable once PossibleInvalidOperationException
						.ToDictionary(dc => dc.TopicID.Value);
			}

			return PartialView("~/Views/Home/_Topics.cshtml", viewModel);
		}

		public static bool IsTopicOnDashboard(DataContext db, int cuid, Topic t)
		{
			var cutoff = DateTime.Now.AddDays(3);
			var normalLogic = !t.IsReadOnly && (t.ResubmissionDate == null || t.ResubmissionDate < cutoff) &&
							(t.OwnerID == cuid || t.Votes.Any(v => v.Voter.ID == cuid));

			var overrideLogic = db.TopicVisibilityOverrides.FirstOrDefault(o => o.TopicID == t.ID && o.UserID == cuid)?.toBool();
			return overrideLogic ?? normalLogic;
		}
		

		/// <summary>
		///    Diese Methode rendert ein einzelnes Thema und gibt dieses zurück.
		/// </summary>
		/// <param name="id">TopicID</param>
		public ActionResult _FetchSingleTopic(int id)
		{
			return PartialView("~/Views/Topics/_Topic.cshtml", db.FilteredTopics(GetRolesForCurrentUser()).First(t => t.ID == id));
		}

        /// <summary>
        ///    Grundlegende Informationen über das IGCV-Protokoll. Diese Seite ist nicht verlinkt.
        /// </summary>
        [AllowAnonymous]
        public ActionResult About()
		{
			return View();
		}
	}
}