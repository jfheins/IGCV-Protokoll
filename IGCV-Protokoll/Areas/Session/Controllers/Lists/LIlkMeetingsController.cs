﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Session.Models.Lists;
using IGCV_Protokoll.util;

namespace IGCV_Protokoll.Areas.Session.Controllers.Lists
{
	public class LIlkMeetingsController : ParentController<IlkMeeting>
	{
		public LIlkMeetingsController()
		{
			_dbSet = db.LIlkMeetings;
			SetAndFilterEntities(_dbSet.OrderBy(im => im.Start).ThenBy(im => im.Place));
		}

		public override PartialViewResult _CreateForm()
		{
			ViewBag.UserList = CreateUserSelectList();
			ViewBag.SessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name");

			return base._CreateForm();
		}

		public override ActionResult _BeginEdit(int id)
		{
			ViewBag.UserList = CreateUserSelectList();
			ViewBag.SessionTypeList = new SelectList(GetActiveSessionTypes(), "ID", "Name");
			return base._BeginEdit(id);
		}

		public override ActionResult Download(int id)
		{
			Debug.Assert(Request.Url != null, "Request.Url != null");

			var IlkMeeting = Entities.Single(e => e.ID == id);
			if (IlkMeeting.GUID == null)
			{
				IlkMeeting.GUID = Guid.NewGuid();
				db.SaveChanges();
			}

			var ical = CreateCalendarEvent("ILK-Regeltermin: " + IlkMeeting.Comments.Shorten(50),
				IlkMeeting.Comments + "\r\n\r\nhttp://" + Request.Url.Authority + Url.Content("~/ViewLists#conference_table"),
				IlkMeeting.Start, IlkMeeting.Start.Date.AddHours(20),
				IlkMeeting.Place, IlkMeeting.GUID.ToString(), false);

			return Content(ical, "text/calendar", Encoding.UTF8);
		}
	}
}