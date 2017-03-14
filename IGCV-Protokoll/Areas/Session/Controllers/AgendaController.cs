using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Session.Models;

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
            var session = GetSession();
            if (session == null)
                return RedirectToAction("Index", "Master");
            
			session.ActiveAgendaItems.Sort(ActiveAgendaItem.PositionComparer);

            return View("Index", session);
        }

        public ActionResult Edit([Bind(Include = "Position,Comment", Prefix = "ActiveAgendaItems")] IEnumerable<ActiveAgendaItem> agendaItems)
        {
            var session = GetSession();
            if (session == null)
                return RedirectToAction("Index", "Master");

	        if (agendaItems == null)
	        {
		        ViewBag.ErrorMessage = "Der Server konnte keine Daten verarbeiten.";
		        return Index();
	        }

            foreach (var editedItem in agendaItems)
            {
                session.ActiveAgendaItems.First(aai => aai.Position == editedItem.Position).Comment = editedItem.Comment;
            }
            db.SaveChanges();
            return RedirectToAction("Index", "Discussion");
        }
    }
}