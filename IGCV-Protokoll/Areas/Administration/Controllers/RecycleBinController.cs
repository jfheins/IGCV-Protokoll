using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using IGCV_Protokoll.Controllers;

namespace IGCV_Protokoll.Areas.Administration.Controllers
{
	[DisplayName("Papierkorb")]
	public class RecycleBinController : BaseController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.AdminStyle = "active";
			ViewBag.ARBStyle = "active";
		}

		// GET: Administration/RecycleBin
		public ActionResult Index()
		{
			var roles = GetRolesForCurrentUser();
			var items = db.FilteredDocumentContainers(roles).Where(dc => dc.Orphaned != null);
			return View(items.ToList());
		}

		[HttpGet]
		public ActionResult Purge()
		{
			var roles = GetRolesForCurrentUser();
			var itemcount = db.FilteredDocumentContainers(roles).Count(dc => dc.Orphaned != null);
			return View(itemcount);
		}

		[HttpPost, ActionName("Purge")]
		[ValidateAntiForgeryToken]
		public ActionResult PurgeConfirmed()
		{
			var roles = GetRolesForCurrentUser();
			var items = db.FilteredDocumentContainers(roles).Where(dc => dc.Orphaned != null);

			var dcController = new DocumentContainerController();
			foreach (var dc in items)
			{
				dcController._PermanentDelete(dc.ID);
			}
			return RedirectToAction("Index");
		}
	}
}