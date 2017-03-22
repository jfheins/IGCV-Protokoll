using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using IGCV_Protokoll.Areas.Session.Models;

namespace IGCV_Protokoll.Models
{
	public class TopicVisibilityOverride
	{
		public int ID { get; set; }


		public int TopicID { get; set; }

		[ForeignKey("TopicID")]
		public virtual Topic Topic { get; set; }

		public int UserID { get; set; }

		[ForeignKey("UserID")]
		public virtual User User { get; set; }
		
		public VisibilityOverride Visibility { get; set; }

		public bool toBool()
		{
			return Visibility == VisibilityOverride.Show;
		}
	}

	public enum VisibilityOverride { Hide, Show }
}