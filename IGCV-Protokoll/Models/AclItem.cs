using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace IGCV_Protokoll.Models
{
	/// <summary>
	/// Access control item
	/// ACL = Access control list
	/// Eine ACL besteht aus beliebig vielen AcItems, die Anwesenheit eines solchen Items signalisiert die Berechtigung.
	/// </summary>
	public class ACLItem
	{
		public int ID { get; set; }

		[Index("IX_ACLEntityunique", 1, IsUnique = true)]
		public int ParentId { get; set; }
		public ACL Parent { get; set; }

		[Index("IX_ACLEntityunique", 2, IsUnique = true)]
		public int AdEntityID { get; set; }
		public AdEntity AdEntity { get; set; }
	}
}