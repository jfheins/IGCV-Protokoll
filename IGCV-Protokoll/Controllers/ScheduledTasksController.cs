using System;
using System.Linq;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Administration.Controllers;

namespace IGCV_Protokoll.Controllers
{
	public class ScheduledTasksController : BaseController
	{
		private readonly TimeSpan _reminderTimeSpan = new TimeSpan(6, 0, 0, 0);

		// GET: ScheduledTasks
		[AllowAnonymous]
		public string Index()
		{
			var cutoff = DateTime.Now;
			foreach (var topic in db.Topics.Where(t => t.ResubmissionDate < cutoff))
				topic.ResubmissionDate = null;

			// Zu lange Sperren werden zurückgesetzt
			cutoff = DateTime.Now.AddHours(-8);
			foreach (var doc in db.Documents.Where(d => d.LockTime < cutoff))
				AttachmentsController.ForceReleaseLock(doc.ID);

			db.SaveChanges();

			// Datenbank mit Active-Directory synchronisieren
			new UserController().PullADEntities();
			
			return AssignmentsController.SendReminders(db);
		}
	}
}