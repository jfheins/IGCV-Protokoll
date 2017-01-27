using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGCV_Protokoll.DataLayer;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.util
{
	public interface IAccessible
	{
		/// <summary>
		/// Falls eine ACL definiert ist, steht hier die ID der ACL. Falls ein Objekt global sichtbar ist, null.
		/// </summary>
		int? AclID { get; set; }

		ACL Acl { get; set; }
	}
}
