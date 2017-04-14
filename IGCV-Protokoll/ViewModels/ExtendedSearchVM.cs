using System.Collections.Generic;
using System.ComponentModel;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.ViewModels
{
	public class ExtendedSearchVM
	{
		public ExtendedSearchVM()
		{
			SearchTopics = false;
			SearchComments = false;
			SearchAssignments = false;
			SearchAttachments = false;
			SearchDecisions = false;
			SearchLists = false;

			Tags = new Dictionary<Tag, bool>();
		}

		[DisplayName("Suchbegriff")]
		public string Searchterm { get; set; }

		[DisplayName("Themen")]
		public bool SearchTopics { get; set; }

		[DisplayName("Kommentare")]
		public bool SearchComments { get; set; }

		[DisplayName("Aufgaben")]
		public bool SearchAssignments { get; set; }

		[DisplayName("Anhänge")]
		public bool SearchAttachments { get; set; }

		[DisplayName("Entscheidungen")]
		public bool SearchDecisions { get; set; }

		[DisplayName("Listeneinträge")]
		public bool SearchLists { get; set; }

		public Dictionary<Tag, bool> Tags { get; set; }
	}
}