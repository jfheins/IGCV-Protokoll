using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IGCV_Protokoll.Models
{
	public class Revision
	{
		public int ID { get; set; }
		public Guid GUID { get; set; }

		[InverseProperty("Revisions")]
		public Document ParentDocument { get; set; }

		[ForeignKey("ParentDocument")]
		public int ParentDocumentID { get; set; }

		[Required]
		[Display(Name = "Uploaddatum")]
		public DateTime Created { get; set; }

		[Display(Name = "Autor")]
		public virtual User Uploader { get; set; }

		[ForeignKey("Uploader")]
		public int UploaderID { get; set; }


		[Required]
		[Display(Name = "Dateigröße")]
		[UIHint("FileSize")]
		public int FileSize { get; set; }


		/// <summary>
		///    Enthält den sicheren Namen der für die Speicherung auf dem Server verwendet wird. Alle unsicheren Zeichen wurden
		///    entfernt.
		/// </summary>
		[Required(AllowEmptyStrings = true)]
		[ScaffoldColumn(false)]
		public string SafeName { get; set; }

		/// <summary>
		///    Enthält die Dateiendung ohne führenden Punkt.
		/// </summary>
		[Required(AllowEmptyStrings = true)]
		[ScaffoldColumn(false)]
		public string Extension { get; set; }

		/// <summary>
		/// Enthält den Namen der Datei im Dateisystem. Falls kein Abweichender Dateiname eingetragen ist, ergibt dieser sich aus der ID und dem SafeName.
		/// </summary>
		[NotMapped]
		public string FileName
		{
			get
			{
				if (DiskName != null)
					return DiskName;
				if (string.IsNullOrWhiteSpace(Extension))
					return ID + "_" + SafeName;
				return ID + "_" + SafeName + '.' + Extension;
			}
		}

		/// <summary>
		/// Gibt einen, vom Standard abweichenden, Dateinamen vor. Fall ungleich null, ist dieser maßgeblich.
		/// </summary>
		public string DiskName { get; set; }
	}
}