﻿using System.Web.Mvc;
using ILK_Protokoll.Areas.Session.Models.Lists;

namespace ILK_Protokoll.Areas.Session.Controllers.Lists
{
	public class LConferencesController : ParentController<Conference>
	{
		public LConferencesController()
		{
			_dbSet = db.LConferences;
		}

		public override PartialViewResult _CreateForm()
		{
			ViewBag.UserList = new SelectList(db.GetUserOrdered(GetCurrentUser()), "ID", "ShortName");
			return base._CreateForm();
		}

		public override ActionResult _BeginEdit(int id)
		{
			ViewBag.UserList = new SelectList(db.GetUserOrdered(GetCurrentUser()), "ID", "ShortName");
			return base._BeginEdit(id);
		}
	}
}