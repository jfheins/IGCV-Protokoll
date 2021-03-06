﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.util;

namespace IGCV_Protokoll.Areas.Session.Models.Lists
{
	/// <summary>
	///    Vakante Stellen
	/// </summary>
	[Table("L_Opening")]
	public class Opening : BaseItem, IFileContainer
	{
		public Opening()
		{
			Start = DateTime.Today;
		}

		[Required]
		[DisplayName("Projekt")]
		public string Project { get; set; }

		[DisplayName("Beginn")]
		[DataType(DataType.Date)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public DateTime Start { get; set; }

		[DisplayName("TG/OE")]
		[Required]
		public string TG { get; set; }

		[DisplayName("Prof.")]
		public Prof Prof { get; set; }

		[Required]
		[DisplayName("Beschreibung / Profil")]
		[DataType(DataType.MultilineText)]
		public string Description { get; set; }

		[DisplayName("Dokumente")]
		public virtual DocumentContainer Documents { get; set; }

		[ForeignKey("Documents")]
		public int DocumentsID { get; set; }

		public string getTitle() => $"Vakante Stelle: {Description}";
	}
}