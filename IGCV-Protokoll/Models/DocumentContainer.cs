using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		// Optional ist ein Topic vorhanden. Falls kein Topic verlinkt ist, gehört dieser Container zu einem Listenelement.
		[Display(Name = "Thema")]
		[Index(IsUnique = true)]
		[ForeignKey("Topic")]
		public int? TopicID { get; set; }

		[InverseProperty("DocumentContainer")]
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

		/// <summary>
		/// Listet nur die nicht-gelöschten Dokumente
		/// </summary>
		public IEnumerable<Document> VisibleDocuments => Documents.Where(d => d.Deleted == null);

		/// <summary>
		/// Titel des Elternelements. Ist nur gesetzt, wenn der Container verwaist ist.
		/// </summary>
		public string Title { get; set; }

		[DisplayName("Zugriffsrechte")]
		public int? AclID { get; set; }
		public ACL Acl { get; set; }

		[NotMapped]
		[UIHint("FileSize")]
		[Display(Name = "Gesamtgröße")]
		public int FileSizeSum
		{
			get { return Documents.Sum(doc => doc.FileSizeSum); }
		}
	}
}