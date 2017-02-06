using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IGCV_Protokoll.Models;
using JetBrains.Annotations;

namespace IGCV_Protokoll.ViewModels
{
	public class AccessControlDisplayViewModel
	{
		[CanBeNull]
		public ICollection<User> AuthorizedUsers { get; set; }
	}

	public class AccessControlEditorViewModel
	{
		public bool IsNewAcl { get; set; }

		[NotNull]
		public string HtmlName { get; set; }

		[NotNull]
		public Dictionary<AdEntity, bool> AuthorizedEntities { get; set; }
	}

	public class SelectedAdEntity
	{
		/// <summary>
		/// ID der ADEntity
		/// </summary>
		public int id { get; set; }

		public bool selected { get; set; }
	}
}