using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using IGCV_Protokoll.util;
using JetBrains.Annotations;

namespace IGCV_Protokoll.Models
{
	public class DocumentContainer : IAccessible
	{
		public DocumentContainer()
		{
			Documents = new List<Document>();
		}
		public int ID { get; set; }

		//----------------------------------------------------------------------------------------------------
		[Display(Name = "Diskussion")]
		[InverseProperty("Attachments")]
		public int? TopicID { get; set; }

		[ForeignKey("TopicID")]
		public virtual Topic Topic { get; set; }
		//----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Enthält das Löschdatum, falls das Elternelement gelöscht wurde, sonst null.
		/// Dies ist eine Abweichung von der Normalform, weil der Zustand eigentlich bereits dadurch definiert ist,
		/// dass keine Element auf diesen Container verweist.
		/// </summary>
		[Display(Name = "Gelöscht")]
		public DateTime? Orphaned { get; set; }

		[NotNull]
		public virtual ICollection<Document> Documents { get; set; }

		public int? AclID { get; set; }
		public ACL Acl { get; set; }
	}
}