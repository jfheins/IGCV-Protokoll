﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.util;

namespace IGCV_Protokoll.Areas.Session.Models.Lists
{
	/// <summary>
	///    Industrieprojekt
	/// </summary>
	[Table("L_IndustryProject")]
	public class IndustryProject : BaseItem, IFileContainer
	{
		public IndustryProject()
		{
			StartDate = DateTime.Today;
			EndDate = DateTime.Today;
		}

		[Required]
		[DisplayName("Partner")]
		public string Partner { get; set; }

		[Required]
		[DisplayName("Name")]
		public string Name { get; set; }

		[DisplayName("Von")]
		[DataType(DataType.Date)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public DateTime StartDate { get; set; }

		[DisplayName("Bis")]
		[DataType(DataType.Date)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public DateTime EndDate { get; set; }

		[DisplayName("ILK")]
		public virtual User Ilk { get; set; }

		[ForeignKey("Ilk")]
		public int IlkID { get; set; }

		[Required]
		[DisplayName("Summe")]
		[DataType(DataType.Currency)]
		[DisplayFormat(DataFormatString = "{0:D}", ApplyFormatInEditMode = false)]
		public int Amount { get; set; }

		[Required]
		[DisplayName("Status")]
		public IndustryProjectState Status { get; set; }

		[DisplayName("Dokumente")]
		public virtual DocumentContainer Documents { get; set; }

		[ForeignKey("Documents")]
		public int DocumentsID { get; set; }

		public string getTitle() => $"Industrieprojekt: {Name} ({Partner})";
	}

    public enum IndustryProjectState
    {
        [Display(Name = "Angefragt")] Inquired,
        [Display(Name = "Angebotserstellung")] Offered,
        [Display(Name = "In Bearbeitung")] InProgress,
        [Display(Name = "Abgeschlossen")] Complete
    }
}