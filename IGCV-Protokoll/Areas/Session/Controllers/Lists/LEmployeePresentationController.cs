using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Session.Models.Lists;

namespace IGCV_Protokoll.Areas.Session.Controllers.Lists
{
	public class LEmployeePresentationsController : ParentController<EmployeePresentation>
	{
		public LEmployeePresentationsController()
		{
			_dbSet = db.LEmployeePresentations;
			SetAndFilterEntities(_dbSet.Include(ep => ep.Documents).OrderByDescending(x => x.Selected).ThenBy(x => x.LastPresentation));
		}

		public override PartialViewResult _List(bool reporting = false)
		{
			CleanupLocks();

			ViewBag.Reporting = reporting;
			var items = Entities.ToList();
			foreach (var emp in items)
			{
				emp.FileCount = emp.Documents.Documents.Count(a => a.Deleted == null);
				if (emp.FileCount > 0)
				{
					var document = emp.Documents.Documents.Where(a => a.Deleted == null).OrderByDescending(a => a.Created).First();
					emp.FileURL = Url.Action("DownloadNewest", "Attachments", new {Area = "", id = document.GUID});
				}
			}
			return PartialView(items);
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

		public override PartialViewResult _FetchRow(int id)
		{
			var emp = Entities.Single(m => m.ID == id);
			var session = GetSession();
			if (session != null && emp.LockSessionID == session.ID) // ggf. lock entfernen
			{
				emp.LockSessionID = null;
				db.SaveChanges();
			}
			ViewBag.Reporting = false;
			emp.FileCount = emp.Documents.Documents.Count(a => a.Deleted == null);
			if (emp.FileCount > 0)
			{
				var document = emp.Documents.Documents.Where(a => a.Deleted == null).OrderByDescending(a => a.Created).First();
				emp.FileURL = Url.Action("DownloadNewest", "Attachments", new {Area = "", id = document.GUID});
			}

			return PartialView("_Row", emp);
		}
	}
}