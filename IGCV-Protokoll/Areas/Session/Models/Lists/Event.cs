﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.util;

namespace IGCV_Protokoll.Areas.Session.Models.Lists
{
	/// <summary>
	///    Termine und Veranstaltungen, die das ganze Institut betreffen
	/// </summary>
	[Table("L_Event")]
	public class Event : BaseItem, IFileContainer
	{
		private string _organizationUnit;

		public Event()
		{
			StartDate = DateTime.Today;
			EndDate = DateTime.Today;
		}

		[DisplayName("Von")]
		[DataType(DataType.Date)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public DateTime StartDate { get; set; }

		[DisplayName("Bis")]
		[DataType(DataType.Date)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public DateTime EndDate { get; set; }

		[Required(AllowEmptyStrings = true)]
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		[DisplayName("Uhrzeit")]
		public string Time { get; set; }

		[Required(AllowEmptyStrings = true)]
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		[DisplayName("Ort")]
		public string Place { get; set; }

		[Required]
		[DisplayName("Verant.")]
		public string Organizer { get; set; }

		[DisplayName("OE")]
		public string OrganizationUnit
		{
			get { return _organizationUnit; }
			set { _organizationUnit = value?.Replace(" ", "") ?? string.Empty; }
		}

		[Required]
		[DisplayName("Besucher / Thema")]
		[DataType(DataType.MultilineText)]
		public string Description { get; set; }

		[DisplayName("Dokumente")]
		public virtual DocumentContainer Documents { get; set; }

		[ForeignKey("Documents")]
		public int DocumentsID { get; set; }

		public string getTitle() => $"Termin: {Description}";
	}
}