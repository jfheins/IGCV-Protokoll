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

			result.HtmlName = htmlId ?? obj.GetType().Name +  "_acl";
			return PartialView(result);
		}

		public ActionResult SynchronizeActiveDirectory()
		{
			return new UserController().PullADEntities();
		}
	}
}