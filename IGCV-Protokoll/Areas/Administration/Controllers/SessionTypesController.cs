using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Administration.Models;
using IGCV_Protokoll.Areas.Administration.ViewModels;
using IGCV_Protokoll.Controllers;

namespace IGCV_Protokoll.Areas.Administration.Controllers
{
	[DisplayName("Sitzungstypen")]
	public class SessionTypesController : BaseController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.AdminStyle = "active";
			ViewBag.ASTStyle = "active";
		}

		// GET: Administration/SessionTypes
		public ActionResult Index()
		{
			return View(db.SessionTypes.Include(st => st.Agenda).ToList());
		}

		// GET: Administration/SessionTypes/Details/5
		public ActionResult Details(int? id)
		{
			if (id == null)
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			SessionType sessionType = db.SessionTypes.Find(id);
			if (sessionType == null)
				return HttpNotFound();
			return View(sessionType);
		}

		// GET: SessionTypes/Create
		public ActionResult Create()
		{
            var vm = new SessionTypeVM();
            vm.AgendaList = new SelectList(db.AgendaTemplates, "ID", "Name");
            vm.UserDict = CreateUserDictionary(u => false);
			return View(vm);
		}

		// POST: Administration/SessionTypes/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create([Bind(Include = "ID,Name,AgendaID")] SessionTypeVM vm, IEnumerable<int> Attendees)
		{
			if (Attendees == null)
			{
				ModelState.AddModelError("Attendees", "Es müssen Stammteilnehmer ausgewählt werden!");
				Attendees = new List<int>();
			}

			if (ModelState.IsValid)
			{
			    var sessionType = vm.updateModel(new SessionType());
				foreach (int userid in Attendees)
					sessionType.Attendees.Add(db.Users.Find(userid));

				db.SessionTypes.Add(sessionType);
				db.SaveChanges();
				return RedirectToAction("Index");
			}

            vm.AgendaList = new SelectList(db.AgendaTemplates, "ID", "Name", vm.AgendaID);
            vm.UserDict = CreateUserDictionary(u => Attendees.Contains(u.ID));
			return View(vm);
		}

		// GET: Administration/SessionTypes/Edit/5
		public ActionResult Edit(int? id)
		{
			if (id == null)
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			SessionType sessionType = db.SessionTypes.Include(st => st.Agenda).First(st => st.ID == id);
			if (sessionType == null)
				return HttpNotFound();

		    var vm = SessionTypeVM.fromModel(sessionType);

            vm.AgendaList = new SelectList(db.AgendaTemplates, "ID", "Name", vm.AgendaID);
            vm.UserDict = CreateUserDictionary(u => sessionType.Attendees.Contains(u));
			return View(vm);
		}

		// POST: Administration/SessionTypes/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit([Bind(Include = "ID,Name,Active,AgendaID")] SessionTypeVM input, IEnumerable<int> Attendees)
		{
			if (ModelState.IsValid)
			{
				var sessionType = db.SessionTypes.Find(input.ID);
			    input.updateModel(sessionType);
				sessionType.Attendees.Clear();

				if (Attendees != null)
				{
					foreach (var userid in Attendees)
						sessionType.Attendees.Add(db.Users.Find(userid));
				}

				db.SaveChanges();
				return RedirectToAction("Index");
			}

            input.AgendaList = new SelectList(db.AgendaTemplates, "ID", "Name", input.AgendaID);
            input.UserDict = CreateUserDictionary(u => Attendees.Contains(u.ID));
			return View(input);
		}

		// GET: Administration/SessionTypes/Delete/5
		public ActionResult Delete(int? id)
		{
			if (id == null)
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			SessionType sessionType = db.SessionTypes.Find(id);
			if (sessionType == null)
				return HttpNotFound();

			if (sessionType.Attendees.Count == 0)
				return View(sessionType);
			else
				return View("DeleteHint", sessionType);
		}

		// POST: Administration/SessionTypes/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public ActionResult DeleteConfirmed(int id)
		{
			SessionType sessionType = db.SessionTypes.Find(id);

			if (sessionType.Attendees.Count != 0)
				return View("DeleteHint", sessionType);

			db.SessionTypes.Remove(sessionType);
			db.SaveChanges();
			return RedirectToAction("Index");
		}
	}
}