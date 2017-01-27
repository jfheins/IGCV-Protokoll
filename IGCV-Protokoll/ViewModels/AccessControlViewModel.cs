using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.ViewModels
{
	public class AccessControlViewModel
	{
		public ICollection<User> AuthorizedUsers { get; set; }
	}
}