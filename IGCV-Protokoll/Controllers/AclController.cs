using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IGCV_Protokoll.util;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.ViewModels;

namespace IGCV_Protokoll.Controllers
{
	public class AclController : BaseController
	{
		public ActionResult _DisplayAuthorized(IAccessible obj)
		{
			var authorizedUsers = db.GetACL(obj)
				.Select(item => item.AdEntity)
				.SelectMany(ade => ade.Users)
				.Select(adu => adu.User).ToList();

			Dictionary<AdEntity, bool> adEntities;
			if (obj.Acl == null)
			{
				obj.Acl = db.ACLs.Add(new ACL());
				db.SaveChanges();
				adEntities = db.AdEntities.ToDictionary(x => x, x => true);
			}
			else
			{
				adEntities = (from adEntity in db.AdEntities
					from aclitem in adEntity.Acl.Where(aclitem => aclitem.ParentId == obj.AclID.Value).DefaultIfEmpty()
					select new {Entity = adEntity, hasAccess = aclitem != null}).ToDictionary(x => x.Entity, x => x.hasAccess);
			}

			return View(new AccessControlViewModel
			{
				AuthorizedUsers = authorizedUsers,

			});
		}

		public ActionResult SynchronizeActiveDirectory()
		{
			

		}

		public ActionResult Authorize([Bind(Prefix = "id")]int aclId, int adEntityId)
		{
			var acl = db.ACLs.Find(aclId);

			// TODO Prüfen, ob der aktuelle Benutzer autorisiert ist

			if (!db.ACLItems.Any(aclitem => aclitem.ParentId == aclId && aclitem.AdEntityID == adEntityId))
				db.ACLItems.Add(new ACLItem { AdEntityID = adEntityId, ParentId = aclId });

			try
			{
				db.SaveChanges();
			}
			catch (SqlException ex) when (ex.Number == 2601)
			{
				// Violation of unique constraint, the entry was added already
			}

			return View(new AccessControlViewModel
			{
				AuthorizedUsers = null
			});
		}
	}
}