﻿using System.Web.Mvc;

namespace IGCV_Protokoll
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
			//filters.Add(new RequireHttpsAttribute());
			filters.Add(new AuthorizeAttribute { Roles = @"IGCV\V-AL, IGCV\Protokoll-Developer" });
		}
	}
}