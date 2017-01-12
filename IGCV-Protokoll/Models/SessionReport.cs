﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using IGCV_Protokoll.Areas.Administration.Models;
using IGCV_Protokoll.Areas.Session.Models;

namespace IGCV_Protokoll.Models
{
	public class SessionReport
	{
		public static readonly string Directory = ConfigurationManager.AppSettings["ReportPath"];

		public SessionReport()
		{
			PresentUsers = new List<User>();
		}

		public int ID { get; set; }

		[DisplayName("Sitzungsleiter")]
		public virtual User Manager { get; set; }

		[ForeignKey("Manager")]
		public int ManagerID { get; set; }

		[DisplayName("Sitzungstyp")]
		[Required]
		public virtual SessionType SessionType { get; set; }

		[DisplayName("Anwesenheit")]
		[Required]
		public virtual ICollection<User> PresentUsers { get; set; }

		[DisplayName("Weitere Personen")]
		public string AdditionalAttendees { get; set; }

		[DisplayName("Notizen")]
		[DataType(DataType.MultilineText)]
		public string Notes { get; set; }

		[DisplayName("Beginn")]
		[Required]
		public DateTime Start { get; set; }

		[DisplayName("Ende")]
		[Required]
		public DateTime End { get; set; }

		[DisplayName("Bearbeitete Themen")]
		public virtual ICollection<Decision> Decisions { get; set; }

		[NotMapped]
		public string FileName
		{
			get { return string.Format("Sessionreport_{0}_{1}.pdf", Start.Year, ID); }
		}

		public static SessionReport FromActiveSession(ActiveSession a)
		{
			return new SessionReport
			{
				Manager = a.Manager,
				SessionType = a.SessionType,
				PresentUsers = a.PresentUsers,
				AdditionalAttendees = a.AdditionalAttendees,
				Notes = a.Notes,
				Start = a.Start,
				End = a.End,
				Decisions = new List<Decision>()
			};
		}
	}
}