using System.Linq;
using IGCV_Protokoll.Areas.Session.Models.Lists;

namespace IGCV_Protokoll.Areas.Session.Controllers.Lists
{
	public class LExtensionsController : ParentController<Extension>
	{
		public LExtensionsController()
		{
			_dbSet = db.LExtensions;
			Entities = _dbSet.OrderBy(ext => ext.EndDate).ThenBy(ext => ext.Employee);
		}
	}
}