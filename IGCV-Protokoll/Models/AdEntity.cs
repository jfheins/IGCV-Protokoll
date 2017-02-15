using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Web;
using JetBrains.Annotations;

namespace IGCV_Protokoll.Models
{
	/// <summary>
	/// Kann eine Gruppe oder ein Benutzer sein.
	/// </summary>
	public class AdEntity
	{
		public AdEntity()
		{
			Name = "";
			SamAccountName = "";
			Children = new List<AdEntity>();
		}
		public AdEntity(Principal p) : this()
		{
			SetTypeByPrincipal(p);
		}

		public void SetTypeByPrincipal(Principal p)
		{
			if (p is UserPrincipal)
				Type = AdEntityType.User;
			else if (p is GroupPrincipal)
				Type = AdEntityType.Group;
			else
				Type = AdEntityType.Other;
		}

		public int ID { get; set; }

		public int? ParentID { get; set; }
		public AdEntity Parent { get; set; }
		[InverseProperty("Parent")]
		public virtual ICollection<AdEntity> Children { get; set; }

		[Required]
		[Index("guid_index", IsUnique = true)]
		public Guid Guid { get; set; }

		[NotNull]
		[Required]
		public string Name { get; set; } // PrincipalName

		[Required]
		public string SamAccountName { get; set; }

		[InverseProperty("AdEntity")]
		public ICollection<AdEntityUser> Users { get; set; }

		[InverseProperty("AdEntity")]
		public virtual ICollection<ACLItem> Acl { get; set; }

		public AdEntityType Type { get; set; }
	}

	public enum AdEntityType { Other, Group, User }

	// Join-Table
	public class AdEntityUser
	{
		public int ID { get; set; }

		public int AdEntityID { get; set; }
		public virtual AdEntity AdEntity { get; set; }

		public int UserID { get; set; }
		public virtual User User { get; set; }
	}
}