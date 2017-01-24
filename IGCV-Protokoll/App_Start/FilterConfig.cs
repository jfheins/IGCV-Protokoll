using System.Web.Mvc;
using IGCV_Protokoll.util;

namespace IGCV_Protokoll
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
			//filters.Add(new RequireHttpsAttribute());
			filters.Add(new PermissiveAuthorizationAttribute { Roles = @"IGCV\V-AL, IGCV\Protokoll-Developer", Users = @"IGCV\Schilpjo, IGCV\Reinhart" });
		}
	}
}