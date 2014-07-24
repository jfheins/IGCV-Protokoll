﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ILK_Protokoll.Areas.Administration.Models;
using ILK_Protokoll.Areas.Session.Models;

namespace ILK_Protokoll.Models
{
	public class SessionReport
	{
		public SessionReport()
		{
			PresentUsers = new List<User>();
		}

		public static SessionReport FromActiveSession(ActiveSession a)
		{
			return new SessionReport()
			{
				Manager = a.Manager,
				SessionType = a.SessionType,
				PresentUsers = a.PresentUsers,
				AdditionalAttendees = a.AdditionalAttendees,
				Notes = a.Notes,
				Start = a.Start,
				End = DateTime.Now
			};
		}

		public int ID { get; set; }

		[DisplayName("Sitzungsleiter")]
		[Required]
		public User Manager { get; set; }

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

		[NotMapped]
		public string URL
		{
			get { return string.Format(@"\\02mucilk\Reports\Sessionreport_{0}_{1}.pdf", Start.Year, ID); }
		}
	}
}