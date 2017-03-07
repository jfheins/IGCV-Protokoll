using System.Linq;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Administration.Controllers;
using IGCV_Protokoll.util;

namespace IGCV_Protokoll
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
            //filters.Add(new RequireHttpsAttribute());
            filters.Add(ConstructFilter());
        }

		private static object ConstructFilter()
		{
			var domain = UserController.DomainName;
			var roles = UserController.AuthorizeGroups.Select(x => domain + '\\' + x).ToList();
			var users = UserController.AuthorizeUsers.Select(x => domain + '\\' + x).ToList();

#if DEBUG
			users.Add(@"IGCV\hz");
			users.Add(@"JULIUS-DESKTOP\Julius");
#endif

			var rolestr = string.Join(", ", roles);
			var userstr = string.Join(", ", users);

			return new PermissiveAuthorizationAttribute {Roles = rolestr, Users = userstr};
		}
    }
}