using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.Controllers
{
	public class SessionReportsController : BaseController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.SessionReportStyle = "active";
		}

		// GET: SessionReports
		public ActionResult Index()
		{
			var cuid = GetCurrentUserID();
			var allowedSessionTypes = db.SessionTypes.Where(st => st.Attendees.Any(a => a.ID == cuid)).Select(st => st.ID).ToArray();
			var visibleReports = db.SessionReports.Include(sr => sr.Manager).Where(sr => allowedSessionTypes.Contains(sr.SessionType.ID)).ToList();
			return View(visibleReports);
		}

		// GET: SessionReports/Details/5
		public ActionResult Details(int? id)
		{
			if (id == null)
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

			SessionReport sessionReport = db.SessionReports.Find(id);
			var cuid = GetCurrentUserID();
			var allowedSessionTypes = db.SessionTypes.Where(st => st.Attendees.Any(a => a.ID == cuid)).Select(st => st.ID).ToArray();

			if (sessionReport == null)
				return HttpNotFound();
			if (!allowedSessionTypes.Contains(sessionReport.SessionType.ID))
				return HTTPStatus(HttpStatusCode.Forbidden, "Nur Stammteilnehmer dürfen Sitzungsprotokolle abrufen.");

			return File(SessionReport.Directory + sessionReport.FileName, "application/pdf");
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				db.Dispose();
			base.Dispose(disposing);
		}
	}
}