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
            
            return View(session);
        }

        public ActionResult Edit([Bind(Include = "Position,Comment")] IEnumerable<ActiveAgendaItem> agendaItems)
        {
            var session = GetSession();
            if (session == null)
                return RedirectToAction("Index", "Master");

            foreach (var editedItem in agendaItems)
            {
                session.ActiveAgendaItems.First(aai => aai.Position == editedItem.Position).Comment = editedItem.Comment;
            }
            db.SaveChanges();
            return RedirectToAction("Index", "Discussion");
        }
    }
}