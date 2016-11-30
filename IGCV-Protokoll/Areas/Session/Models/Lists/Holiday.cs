using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.Areas.Session.Models.Lists
{
	/// <summary>
	///    ILK-Urlaub
	/// </summary>
	[Table("L_Holiday")]
	public class Holiday : BaseItem
	{
		[DisplayName("Person")]
		public virtual User Person { get; set; }

		[ForeignKey("Person")]
		public int PersonID { get; set; }

		[Required]
		[DisplayName("Anlass")]
		public string Occasion { get; set; }


		[DisplayName("Beginn")]
		[DataType(DataType.Date)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public DateTime? Start { get; set; }

		[DisplayName("Ende")]
		[DataType(DataType.Date)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public DateTime? End { get; set; }
	}
}