using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace IGCV_Protokoll.Models
{
	public class TopicLink
	{
		public int ID { get; set; }

		public int LeftTopicID { get; set; }
		[ForeignKey("LeftTopicID")]
		public virtual Topic LeftTopic { get; set; }

		public int RightTopicID { get; set; }
		[ForeignKey("RightTopicID")]
		public virtual Topic RightTopic { get; set; }
		
		public int LinkTemplateID { get; set; }
		[ForeignKey("LinkTemplateID")]
		public TopicLinkTemplate LinkTemplate { get; set; }
	}

	public class TopicLinkTemplate
	{
		public int ID { get; set; }

		[Required]
		public string LeftText { get; set; }
		[Required]
		public string RightText { get; set; }
	}
}