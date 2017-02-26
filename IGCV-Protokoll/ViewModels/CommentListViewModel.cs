using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.ViewModels
{
	public class CommentListViewModel
	{
		public List<Comment> Items { get; set; }
		public int ParentTopicID { get; set; }
		public bool ShowCreateForm { get; set; }
		public bool AllowDeletion { get; set; }
	}
}