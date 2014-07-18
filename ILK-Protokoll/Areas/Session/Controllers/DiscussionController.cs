﻿using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace ILK_Protokoll.Areas.Session.Controllers
{
	public class DiscussionController : SessionBaseController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.SDiscussionStyle = "active";
		}

		// GET: Session/Topic
		public ActionResult Index()
		{
			var session = GetSession();
			if (session == null)
				return RedirectToAction("Index", "Master");

			var topics = db.Topics
				.Include(t => t.SessionType)
				.Include(t => t.TargetSessionType)
				.Include(t => t.Owner)
				.Include(t => t.Comments)
				.Where(t => t.Lock.Session.ID == session.ID || t.SessionTypeID == session.SessionType.ID)
				.OrderByDescending(t => t.Priority)
				.ThenBy(t => t.Created).ToList();

			return View(topics);
		}
	}
}