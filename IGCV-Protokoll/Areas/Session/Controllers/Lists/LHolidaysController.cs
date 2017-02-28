using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Session.Models.Lists;

namespace IGCV_Protokoll.Areas.Session.Controllers.Lists
{
	public class LHolidaysController : ParentController<Holiday>
	{
		public LHolidaysController()
		{
			_dbSet = db.LHolidays;
			SetAndFilterEntities(_dbSet.Include(h => h.Person).OrderBy(h => h.Start).ThenBy(h => h.Person.LongName));
		}

		public override PartialViewResult _CreateForm()
		{
			ViewBag.UserList = CreateUserSelectList();
			return base._CreateForm();
		}

		public override ActionResult _BeginEdit(int id)
		{
			ViewBag.UserList = CreateUserSelectList();
			return base._BeginEdit(id);
		}
	}
}