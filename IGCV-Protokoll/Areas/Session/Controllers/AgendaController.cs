using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IGCV_Protokoll.Areas.Session.Controllers
{
    public class AgendaController : SessionBaseController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewBag.SAgendaStyle = "active";
        }

        // GET: Session/Agenda
        public ActionResult Index()
        {
            return View();
        }
    }
}