﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.Controllers
{
	public class PushController : BaseController
	{
		// GET: Push
		public PartialViewResult _EditListTopic(Topic topic)
		{
			var targets = new HashSet<int>(topic.PushTargets.Select(pt => pt.UserID));
			var pushlist = CreateUserDictionary(u => targets.Contains(u.ID));
			ViewBag.TopicID = topic.ID;

			return PartialView("_EditListTopic", pushlist);
		}

		public ActionResult _RegisterPushTargets(int topicID, Dictionary<int, bool> users)
		{
			var topic = db.Topics.Find(topicID);

			if (topic == null)
				return HTTPStatus(HttpStatusCode.InternalServerError, "Das Thema zu diesem Kommentar konnte nicht gefunden werden.");
			if (!IsAuthorizedFor(topic))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");
			if (topic.IsReadOnly)
				return HTTPStatus(HttpStatusCode.Forbidden, "Das Thema ist schreibgeschützt.");

			var desired = new HashSet<int>(users.Where(kvp => kvp.Value).Select(kvp => kvp.Key));
			var actual = new HashSet<int>(db.PushNotifications.Where(pn => pn.TopicID == topicID).Select(pn => pn.UserID));

			foreach (var userID in desired.Except(actual))
				db.PushNotifications.Add(new PushNotification
				{
					TopicID = topicID,
					UserID = userID
				});

			foreach (var userID in actual.Except(desired))
				db.PushNotifications.RemoveRange(db.PushNotifications.Where(pn => pn.TopicID == topicID && pn.UserID == userID));

			db.SaveChanges();

			return _EditListTopic(topic);
		}

		public ActionResult _ConfirmRead(int id)
		{
			var pn = db.PushNotifications.Find(id);
			pn.Confirmed = true;
			db.SaveChanges();
			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}

		public ActionResult _DisplayListTopic(Topic topic)
		{
			var notifications = db.PushNotifications.Where(pn => pn.TopicID == topic.ID)
				.ToDictionary(pn => pn.User, pn => pn.Confirmed);

			return PartialView("_DisplayListTopic", notifications);
		}
	}
}