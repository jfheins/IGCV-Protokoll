using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IGCV_Protokoll.Models
{
	public class ACL
	{
		public int ID { get; set; }

		public virtual ICollection<ACLItem> Items { get; set; }
	}
}