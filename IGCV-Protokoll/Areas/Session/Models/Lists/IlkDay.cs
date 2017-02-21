using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IGCV_Protokoll.Areas.Administration.Models;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.Areas.Session.Models.Lists
{
	/// <summary>
	///    ILK-Tage
	/// </summary>
	[Table("L_IlkDay")]
	public class IlkDay : BaseItem
	{
		public IlkDay()
		{
			Start = DateTime.Today.AddHours(9);
			End = DateTime.Today.AddHours(17);
		}

		[DisplayName("Beginn")]
		[DataType(DataType.DateTime)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
		public DateTime Start { get; set; }

		[DisplayName("Ende")]
		[DataType(DataType.DateTime)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
		public DateTime End { get; set; }

		[Required]
		[DisplayName("Ort")]
		public string Place { get; set; }

		[DisplayName("Sitzung")]
		public virtual SessionType SessionType { get; set; }

		[DisplayName("Sitzung")]
		[ForeignKey("SessionType")]
		public int SessionTypeID { get; set; }

		[DisplayName("Organisator")]
		public virtual User Organizer { get; set; }

		[ForeignKey("Organizer")]
		public int OrganizerID { get; set; }

		[Required]
		[DisplayName("Themen")]
		[DataType(DataType.MultilineText)]
		public string Topics { get; set; }

		[Required]
		[DisplayName("Teilnehmer")]
		public string Participants { get; set; }
	}
}