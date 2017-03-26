using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using IGCV_Protokoll.Controllers;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.ViewModels;
using Newtonsoft.Json;

namespace IGCV_Protokoll.Areas.Administration.Controllers
{
	[DisplayName("Einstellungen")]
	public class SettingsController : BaseController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.AdminStyle = "active";
			ViewBag.ASettingsStyle = "active";
		}

		// GET: Administration/Settings
		[HttpGet]
		public ActionResult Index()
		{
			var user = db.Users.Find(GetCurrentUserID());
			if (user == null)
				return HttpNotFound("User not found");

			user.Settings.AclTreeVM = CreateAclVM(user.Settings.AclTreePresetUsers);

			return View(user.Settings);
		}

		private AccessControlEditorViewModel CreateAclVM(int[] aclPreset)
		{
			return new AccessControlEditorViewModel
			{
				IsNewAcl = aclPreset == null,
				AuthorizedEntities = db.AdEntities.ToDictionary(x => x, x => aclPreset == null || aclPreset.Contains(x.ID)),
				HtmlName = "aclPreset"
			};
		}

		// POST: Administration/Settings
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Save(UserSettings settings, string aclPresetTree)
		{
			// GetCurrentUser() trackt keine Änderungen, daher den User neu aus der DB holen
			var user = db.Users.Find(GetCurrentUserID());

			var selectedAclTree = JsonConvert.DeserializeObject<List<SelectedAdEntity>>(aclPresetTree);
			var newAclTree = selectedAclTree.Where(x => x.selected).Select(x => x.id);
			settings.AclTreePresetUsers = newAclTree.ToArray();

			user.Settings = settings;
			db.SaveChanges();
			Session["CurrentUser"] = null;
			return RedirectToAction("Index");
		}
	}
}