using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Linq;
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
		public ActionResult _DisplayAuthorized(IAccessible obj)
		{
			ICollection<User> authorizedUsers = null;
			if (obj.Acl != null)
			{
				authorizedUsers = db.GetACL(obj)
					.Select(item => item.AdEntity)
					.SelectMany(ade => ade.Users)
					.Select(adu => adu.User).OrderBy(u => u.ShortName).ToList();
			}

			return PartialView(new AccessControlDisplayViewModel
			{
				AuthorizedUsers = authorizedUsers
			});
		}

		public ActionResult _AclEditorFor(IAccessible obj)
		{
			if (obj == null)
				return new HttpNotFoundResult("Zu editierendes Objekt darf nicht null sein!");

			Dictionary<AdEntity, bool> adEntities;
			if (obj.Acl == null)
			{
				//obj.Acl = db.ACLs.Add(new ACL());
				//db.SaveChanges();
				adEntities = db.AdEntities.ToDictionary(x => x, x => true);
			}
			else
			{
				adEntities = (from adEntity in db.AdEntities
							  from aclitem in adEntity.Acl.Where(aclitem => aclitem.ParentId == obj.AclID.Value).DefaultIfEmpty()
							  select new { Entity = adEntity, hasAccess = aclitem != null }).ToDictionary(x => x.Entity, x => x.hasAccess);
			}

			var root = adEntities.Keys.Single(e => e.ParentID == null);

			return PartialView(new AccessControlEditorViewModel
			{
				RootEntity = root,
				AuthorizedEntities = adEntities
			});
		}

		public ActionResult _AclEditorFor(int aclID)
		{
			var adEntities = (from adEntity in db.AdEntities
							  from aclitem in adEntity.Acl.Where(aclitem => aclitem.ParentId == aclID).DefaultIfEmpty()
							  select new { Entity = adEntity, hasAccess = aclitem != null }).ToDictionary(x => x.Entity, x => x.hasAccess);

			return PartialView(new AccessControlEditorViewModel
			{
				AuthorizedEntities = adEntities
			});
		}

		public ActionResult SynchronizeActiveDirectory()
		{
			return new UserController().PullADEntities();
		}

		public ActionResult Authorize([Bind(Prefix = "id")]int aclId, int adEntityId)
		{
			var acl = db.ACLs.Find(aclId);

			// TODO Prüfen, ob der aktuelle Benutzer autorisiert ist

			if (!acl.Items.Any(aclitem => aclitem.ParentId == aclId && aclitem.AdEntityID == adEntityId))
				db.ACLItems.Add(new ACLItem { AdEntityID = adEntityId, ParentId = aclId });

			try
			{
				db.SaveChanges();
			}
			catch (SqlException ex) when (ex.Number == 2601)
			{
				// Ignore this exception.
				// Violation of unique constraint, the entry was added already
			}

			return View(new AccessControlEditorViewModel
			{
				AuthorizedEntities = null
			});
		}
	}
}