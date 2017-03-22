using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.ViewModels;

namespace IGCV_Protokoll.Controllers
{
	public class TopicLinkController : BaseController
	{
		public ActionResult _List(int topicID)
		{
			var links = db.TopicLinks.Where(l => l.LeftTopicID == topicID)
				.Select(l => new TopicLinkViewModel { Text = l.LinkTemplate.LeftText, Other = l.RightTopic, LinkID = l.ID })
				.Concat(db.TopicLinks.Where(l => l.RightTopicID == topicID)
				.Select(l => new TopicLinkViewModel { Text = l.LinkTemplate.RightText, Other = l.LeftTopic, LinkID = l.ID })).ToList();
			
			return PartialView("_List", links.ToLookup(x => x.Text));
		}

		// AJAX: TopicLink/Create
		public ActionResult _Create(int topicID)
		{
			if (!IsAuthorizedForTopic(topicID))
				return HTTPStatus(HttpStatusCode.Forbidden, "Keine Berechtigung!");

			return PartialView("_Create", CreateVM(topicID));
		}

		private TopicLinkCreateFormViewModel CreateVM(int topicID)
		{
			var linkTypes = db.TopicLinkTemplates.Select(t => new {t.ID, Text = t.LeftText}).Union(
				db.TopicLinkTemplates.Select(t => new {ID = -t.ID, Text = t.RightText}))
				.AsEnumerable().OrderBy(x => Math.Abs(x.ID - 0.1)).ToArray();

			var roles = GetRolesForCurrentUser();
			var topics = db.FilteredTopics(roles).Select(t => new MinimalTopicInfoViewModel {ID = t.ID, Title = t.Title});

			return new TopicLinkCreateFormViewModel
			{
				Left = new MinimalTopicInfoViewModel(db.Topics.Find(topicID)),
				AvailableLinkTypes = new SelectList(linkTypes, "ID", "Text"),
				AvailableTopics = topics.ToList()
			};
		}

		// POST: TopicLink/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(int RightTopicID, int LeftTopicID, int LinkTypeID)
		{
			if (LinkTypeID == 0 || RightTopicID == 0)
				return HTTPStatus(HttpStatusCode.BadRequest, "Parameter dürfen nicht 0 sein!");

			if (LinkTypeID < 0) // Der Benutzer hat die umgekehrte Verknüfungsrichtung gewählt
			{
				LinkTypeID = -LinkTypeID;
				var temp = LeftTopicID;
				LeftTopicID = RightTopicID;
				RightTopicID = temp;
			}

			db.TopicLinks.Add(new TopicLink {LeftTopicID = LeftTopicID, RightTopicID = RightTopicID, LinkTemplateID = LinkTypeID});
			db.SaveChanges();
			return _List(LeftTopicID);
		}

		// POST: TopicLink/Delete/5
		[HttpPost]
		public ActionResult Delete(int id)
		{
			try
			{
				// TODO: Add delete logic here

				return RedirectToAction("Index");
			}
			catch
			{
				return View();
			}
		}
	}
}
