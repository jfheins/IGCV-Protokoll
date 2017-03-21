using System.Collections.Generic;

namespace IGCV_Protokoll.ViewModels
{
	public class AclPreset
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public IEnumerable<string> IncludedUsers { get; set; }
	}
}