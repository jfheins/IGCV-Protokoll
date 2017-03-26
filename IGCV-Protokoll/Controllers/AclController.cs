using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using IGCV_Protokoll.Areas.Administration.Controllers;
using IGCV_Protokoll.DataLayer;
using IGCV_Protokoll.util;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.ViewModels;

namespace IGCV_Protokoll.Controllers
{
	public class AclController : BaseController
	{
		public ActionResult _DisplayAuthorizedFor(IAccessible obj)
		{
			return _DisplayAuthorized(obj.AclID);
		}

		public ActionResult _DisplayAuthorized(int? aclID)
		{
			return PartialView("_DisplayAuthorized", new AccessControlDisplayViewModel
			{
				AuthorizedUsers = aclID.HasValue ? GetAuthorizedUsers(aclID.Value) : null
			});
		}

		private List<User> GetAuthorizedUsers(int aclID)
		{
			return db.ACLItems.Include(i => i.AdEntity).Where(item => item.ParentId == aclID)
				.Select(item => item.AdEntity)
				.SelectMany(ade => ade.Users)
				.Select(adu => adu.User).Distinct().OrderBy(u => u.ShortName).ToList();
		}

		// GET: Acl/Edit/5
		public ActionResult Edit(int? id)
		{
			if (id == null)
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

			return View(id);
		}

		// POST: Acl/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(int id, string aclTree)
		{
			SaveAclForExisting(id, aclTree);
			return Edit(id);
		}


		public ActionResult _AclEditorFor(IAccessible obj, string htmlId)
		{
			if (obj == null)
				return new HttpNotFoundResult("Zu editierendes Objekt darf nicht null sein!");

			var result = new AccessControlEditorViewModel();
			if (obj.Acl == null)
			{
				result.IsNewAcl = true;
				result.AuthorizedEntities = db.AdEntities.ToDictionary(x => x, x => true);
			}
			else
			{
				result.IsNewAcl = false;
				result.AuthorizedEntities = (from adEntity in db.AdEntities
											 from aclitem in adEntity.Acl.Where(aclitem => aclitem.ParentId == obj.AclID.Value).DefaultIfEmpty()
											 select new { Entity = adEntity, hasAccess = aclitem != null }).ToDictionary(x => x.Entity, x => x.hasAccess);
			}

			result.HtmlName = htmlId ?? obj.GetType().Name + "_acl";
			return PartialView(result);
		}

		public ActionResult _StandaloneEditorFor(int aclID)
		{
			if (!IsAuthorizedFor(aclID))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Vorgang nicht berechtigt!");

			var editor = new AccessControlEditorViewModel
			{
				IsNewAcl = false,
				AuthorizedEntities = (from adEntity in db.AdEntities
									  from aclitem in adEntity.Acl.Where(aclitem => aclitem.ParentId == aclID).DefaultIfEmpty()
									  select new { Entity = adEntity, hasAccess = aclitem != null }).ToDictionary(x => x.Entity, x => x.hasAccess),
				HtmlName = "standaloneTree_" + aclID
			};

			var display = new AccessControlDisplayViewModel
			{
				AuthorizedUsers = GetAuthorizedUsers(aclID)
			};

			return PartialView("_StandaloneEditor", new AccessControlEditorDisplayViewModel
			{
				ID = aclID,
				Editor = editor,
				Display =  display
			});
		}

		[HttpPost]
		public ActionResult _SaveExistingAcl(int aclID, string aclTree)
		{
			var result = SaveAclForExisting(aclID, aclTree);

			return result ?? _DisplayAuthorized(aclID);
		}

		public ActionResult SynchronizeActiveDirectory()
		{
			return new UserController().PullADEntities();
		}
	}
}