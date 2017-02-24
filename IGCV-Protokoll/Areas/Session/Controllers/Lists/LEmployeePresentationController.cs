﻿using System;
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
			Entities = _dbSet.Include(ep => ep.Documents).OrderByDescending(x => x.Selected).ThenBy(x => x.LastPresentation);
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

		public ActionResult Edit(int? id, Uri returnURL = null, string statusMessage = null)
		{
			if (id == null)
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

			EmployeePresentation presentation = _dbSet.Include(ep => ep.Documents).Single(ep => ep.ID == id);
			if (presentation == null)
				return HttpNotFound();

			ViewBag.UserList = CreateUserSelectList();
			ViewBag.ReturnURL = returnURL ?? GetFallbackUri();
			ViewBag.StatusMessage = statusMessage;
			return View(presentation);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public virtual ActionResult Edit([Bind(Exclude = "LastChanged")] EmployeePresentation input, Uri returnURL = null)
		{
			ViewBag.UserList = CreateUserSelectList();
			ViewBag.ReturnURL = returnURL ?? GetFallbackUri();

			if (!ModelState.IsValid)
				return View(input);

			db.Entry(input).State = EntityState.Modified;
			db.SaveChanges();
			return RedirectToAction("Edit", "LEmployeePresentations", new
			{
				Area = "Session",
				id = input.ID,
				returnURL,
				statusMessage = "Daten erfolgreich gespeichert."
			});
		}

		private Uri GetFallbackUri()
		{
			var actionuri = Url.Action("Index", "ViewLists", new {Area = ""});
			return new Uri(actionuri ?? "/");
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

		public override ActionResult _Delete(int? id)
		{
			if (id == null)
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

			EmployeePresentation ep = _dbSet.FirstOrDefault(x => x.ID == id.Value);
			if (ep == null)
				return HttpNotFound();
			
			ep.Documents.Orphaned = DateTime.Now;
			ep.Documents = null;
			try
			{
				db.SaveChanges();
			}
			catch (DbEntityValidationException e)
			{
				var msg = ErrorMessageFromException(e);
				return HTTPStatus(HttpStatusCode.InternalServerError, msg);
			}

			_dbSet.Remove(ep);
			db.SaveChanges();

			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}
	}
}