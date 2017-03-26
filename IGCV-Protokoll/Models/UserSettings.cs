using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using IGCV_Protokoll.ViewModels;

namespace IGCV_Protokoll.Models
{
	public class UserSettings
	{
		public UserSettings()
		{
			ColorScheme = ColorScheme.RMV;
			ReportOccasions = SessionReportOccasions.Always;
		}

		[DisplayName("Farbschema")]
		public ColorScheme ColorScheme { get; set; }

		[DisplayName("E-Mail Protokolle")]
		public SessionReportOccasions ReportOccasions { get; set; }

		[DisplayName("Voreinstellung Rechteverwaltung")]
		public string AclTreePreset { get; set; }
		[NotMapped]
		public int[] AclTreePresetUsers
		{
			get
			{
				return AclTreePreset == null ? null : Array.ConvertAll(AclTreePreset.Split(';'), int.Parse);
			}
			set
			{
				AclTreePreset = string.Join(";", value.Select(p => p.ToString()));
			}
		}
		[NotMapped]
		public AccessControlEditorViewModel AclTreeVM { get; set; }
	}

	public enum ColorScheme
	{
		[Display(Name = "RMV Grün")] RMV = 1
	}

	public enum SessionReportOccasions
	{
		[Display(Name = "Immer")] Always,
		[Display(Name = "Bei Abwesenheit")] WhenAbsent,
		[Display(Name = "Niemals")] Never
	}
}