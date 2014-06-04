﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ILK_Protokoll.Models;

namespace ILK_Protokoll.Controllers
{
	public class AttachmentsController : BaseController
	{
		// GET: Attachments
		public ActionResult _List(int? topicID)
		{
			var files = db.Attachments.Where(a => a.TopicID == topicID).Include(a => a.Uploader).ToList();
			ViewBag.TopicID = topicID;
			ViewBag.CurrentUser = GetCurrentUser();
			return PartialView("_AttachmentList", files);
		}

		public PartialViewResult _UploadForm(int topicID)
		{
			var a = new Attachment() { TopicID = topicID };
			return PartialView("_UploadForm", a);
		}
	}
}
