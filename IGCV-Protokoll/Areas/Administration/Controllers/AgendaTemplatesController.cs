using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using EntityFramework.Extensions;
using IGCV_Protokoll.Areas.Session.Models;
using IGCV_Protokoll.Controllers;
using IGCV_Protokoll.DataLayer;

namespace IGCV_Protokoll.Areas.Administration.Controllers
{
    [DisplayName("Agendas")]
    public class AgendaTemplatesController : BaseController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewBag.AdminStyle = "active";
            ViewBag.AAgendaTplStyle = "active";
        }
        // GET: Administration/AgendaTemplates
        public ActionResult Index()
        {
            return View(db.AgendaTemplates.Include(at => at.AgendaItems).ToList());
        }

        // GET: Administration/AgendaTemplates/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AgendaTemplate agendaTemplate = db.AgendaTemplates.Find(id);
            if (agendaTemplate == null)
            {
                return HttpNotFound();
            }
            return View(agendaTemplate);
        }

        // GET: Administration/AgendaTemplates/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Administration/AgendaTemplates/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,Name")] AgendaTemplate agendaTemplate)
        {
            if (ModelState.IsValid)
            {
                db.AgendaTemplates.Add(agendaTemplate);
                db.SaveChanges();
                return RedirectToAction("Edit", new { ID = agendaTemplate.ID });
            }

            return View(agendaTemplate);
        }

        // GET: Administration/AgendaTemplates/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var agendaTemplate = db.AgendaTemplates.Include(at => at.AgendaItems).First(at => at.ID == id);
            if (agendaTemplate == null)
            {
                return HttpNotFound();
            }
            return View(agendaTemplate);
        }

        // POST: Administration/AgendaTemplates/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,Name,AgendaItems")] AgendaTemplate agendaTemplate)
        {
            if (ModelState.IsValid)
            {
                // Alte Punkte löschen
                db.AgendaItems.Where(ai => ai.ParentID == agendaTemplate.ID).Delete();

                for (int i = 0; i < agendaTemplate.AgendaItems.Count; i++)
                {
                    var x = db.AgendaItems.Add(agendaTemplate.AgendaItems[i]);
                    x.ParentID = agendaTemplate.ID;
                    x.Position = i;
                }

                db.Entry(agendaTemplate).State = EntityState.Modified;

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(agendaTemplate);
        }

        // GET: Administration/AgendaTemplates/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AgendaTemplate agendaTemplate = db.AgendaTemplates.Find(id);
            if (agendaTemplate == null)
            {
                return HttpNotFound();
            }
            return View(agendaTemplate);
        }

        // POST: Administration/AgendaTemplates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AgendaTemplate agendaTemplate = db.AgendaTemplates.Find(id);
            db.AgendaTemplates.Remove(agendaTemplate);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
