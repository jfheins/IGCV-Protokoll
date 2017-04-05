using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Administration.Models;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.ViewModels
{
	public class TopicCreateVM : TopicEdit
	{
		public List<AclPreset> AvailableAclPresets { get; set; }
		//public int? SelectedAclPreset { get; set; }

		public Dictionary<int, int[]> MapSTtoAdEntities { get; set; }

		public TopicType TopicType { get; set; }
	}
}