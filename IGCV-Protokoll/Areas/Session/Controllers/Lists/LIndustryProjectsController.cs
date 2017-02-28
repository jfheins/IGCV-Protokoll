using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Session.Models.Lists;
using IGCV_Protokoll.util;

namespace IGCV_Protokoll.Areas.Session.Controllers.Lists
{
	public class LIndustryProjectsController : ParentController<IndustryProject>
	{
		public LIndustryProjectsController()
		{
			_dbSet = db.LIndustryProject;
			SetAndFilterEntities(_dbSet.OrderBy(e => e.StartDate).ThenBy(e => e.Name));
        }



        public override PartialViewResult _CreateForm()
        {
            ViewBag.UserList = CreateUserSelectList();
            return base._CreateForm();
        }

        public override ActionResult _BeginEdit(int id)
        {
            ViewBag.UserList = CreateUserSelectList();
            return base._BeginEdit(id);
        }
	}
}