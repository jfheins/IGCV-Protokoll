using System.Linq;
using IGCV_Protokoll.Areas.Session.Models.Lists;

namespace IGCV_Protokoll.Areas.Session.Controllers.Lists
{
	public class LOpeningsController : ParentController<Opening>
	{
		public LOpeningsController()
		{
			_dbSet = db.LOpenings;
			SetAndFilterEntities(_dbSet.OrderBy(o => o.Start).ThenBy(o => o.Project));
		}
	}
}