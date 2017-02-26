﻿using System.Collections.Generic;
using System.Linq;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.ViewModels
{
	public class CompleteTopicList
	{
		public IEnumerable<Topic> Topics { get; set; }
		public Dictionary<int, bool> IsLocked { get; set; }
		public Dictionary<int, DocumentContainer> Documents { get; set; }
		public ILookup<int, Comment> Comments { get; set; }
	}
}