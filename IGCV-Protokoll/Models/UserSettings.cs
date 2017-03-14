using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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