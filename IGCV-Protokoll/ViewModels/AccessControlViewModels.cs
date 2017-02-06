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
		public AdEntity RootEntity { get; set; }

		[NotNull]
		public Dictionary<AdEntity, bool> AuthorizedEntities { get; set; }
	}
}