using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.ViewModels
{
	public class TopicLinkViewModel
	{
		public int LinkID { get; set; }
		public Topic Other { get; set; }
		public string Text { get; set; }
	}

	public class TopicLinkCreateFormViewModel
	{
		[Display(Name ="Diese Diskussion")]
		public MinimalTopicInfoViewModel Left { get; set; }

		[Display(Name = "Diskussion")]
		public int RightTopicID { get; set; }
		
		[Display(Name = "Verknüpfungsart")]
		public int LinkTypeID { get; set; }

		public SelectList AvailableLinkTypes { get; set; }

		public List<MinimalTopicInfoViewModel> AvailableTopics { get; set; }
	}

	public class MinimalTopicInfoViewModel
	{
		public int ID { get; set; }
		public string Title { get; set; }

		public MinimalTopicInfoViewModel()
		{
		}

		public MinimalTopicInfoViewModel(Topic t)
		{
			ID = t.ID;
			Title = t.Title;
		}
	}
}