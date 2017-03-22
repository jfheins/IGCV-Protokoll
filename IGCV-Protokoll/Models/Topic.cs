using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using IGCV_Protokoll.Areas.Administration.Models;
using IGCV_Protokoll.Areas.Session.Models;
using IGCV_Protokoll.util;
using IGCV_Protokoll.ViewModels;
using JetBrains.Annotations;

namespace IGCV_Protokoll.Models
{
	public class Topic : IAccessible
	{
		public Topic()
		{
			// ReSharper disable DoNotCallOverridableMethodsInConstructor
			Tags = new List<TagTopic>();
			Comments = new List<Comment>();
			Votes = new List<Vote>();
			Assignments = new List<Assignment>();
			Created = DateTime.Now;
			ValidFrom = DateTime.Now;
			UnreadBy = new List<UnreadState>();
			DocumentContainer = new List<DocumentContainer> { };
			// ReSharper restore DoNotCallOverridableMethodsInConstructor
		}

		[Display(Name = "Topic-ID")]
		public int ID { get; set; }

		[Display(Name = "Besitzer")]
		[Required]
		public int OwnerID { get; set; }

		[ForeignKey("OwnerID")]
		public virtual User Owner { get; set; }

		public virtual SessionType SessionType { get; set; }

		[Display(Name = "Sitzungstyp")]
		[Required]
		public int SessionTypeID { get; set; }

		public virtual SessionType TargetSessionType { get; set; }

		[Display(Name = "Zukünftiger Sitzungstyp")] // Falls der DP gerade verschoben wird
		public int? TargetSessionTypeID { get; set; }


		[Display(Name = "Wiedervorlagedatum")]
		[DataType(DataType.Date)]
		[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
		public DateTime? ResubmissionDate { get; set; }

		[Display(Name = "Titel")]
		[Required]
		public string Title { get; set; }

		[Display(Name = "Tags")]
		public virtual ICollection<TagTopic> Tags { get; set; }

		[Display(Name = "Uhrzeit")]
		[Required(AllowEmptyStrings = true)]
		public string Time { get; set; }

		[Display(Name = "Beschreibung")]
		[DataType(DataType.MultilineText)]
		[Required]
		public string Description { get; set; }

		[Display(Name = "Beschlussvorschlag")]
		[DataType(DataType.MultilineText)]
		[Required]
		public string Proposal { get; set; }

		[Display(Name = "Kommentare")]
		public virtual ICollection<Comment> Comments { get; set; }

		[Display(Name = "Stimmen")]
		public virtual ICollection<Vote> Votes { get; set; }

		[Display(Name = "Zur Kenntnisnahme pushen an")]
		public virtual ICollection<PushNotification> PushTargets { get; set; }

		[Display(Name = "Aufgaben")]
		public virtual ICollection<Assignment> Assignments { get; set; }

		[Display(Name = "Entscheidung")]
		public virtual Decision Decision { get; set; }

		[Display(Name = "Priorität")]
		[Required]
		public Priority Priority { get; set; }

		/// <summary>
		/// DO NOT USE
		/// Damit das EF einen Fremdschlüssel in den DokumentenContainer einfügt, muss diese Eigenschaft als
		/// 1:n deklariert werden. Tatsächlich gibt es immer nur genau einen Container pro Thema. Dieser kann
		/// über die Property Documents abferufen werden.
		/// </summary>
		public virtual ICollection<DocumentContainer> DocumentContainer { get; set; }

		[Display(Name = "Dokumente")]
		[NotMapped]
		public DocumentContainer Documents
		{
			get
			{
				var d = DocumentContainer.FirstOrDefault();
				if (d == null)
				{
					d = new DocumentContainer();
					DocumentContainer.Add(d);
				}
				return d;
			}
		}

		[Display(Name = "Erstellt")]
		[Required]
		public DateTime Created { get; set; }

		[Display(Name = "Ersteller")]
		[ForeignKey("CreatorID")]
		public virtual User Creator { get; set; }

		[Required]
		public int CreatorID { get; set; }

		/// <summary>
		///    Gibt das Datum der letzten Änderung an. Falls der Diskussionspunkt nie geändert wurde, gleicht das Änderungsdatum
		///    der Erstelldatum.
		/// </summary>
		[Display(Name = "Geändert")]
		[Required]
		public DateTime ValidFrom { get; set; }

		/// <summary>
		/// Dieses Flag wird während der PDF-Generierung gesetzt im Sitzungsmodus. Die Ausgabe von Diskussionsteilen
		/// sollte dann so erfolgen, dass keine direkte Bearbeitung möglich ist, am Bestern Links komplett weglassen.
		/// </summary>
		[Display(Name = "Schreibschutz")]
		public bool IsReadOnly { get; set; }

		[NotMapped] // Muss bei Bedarf durch den Controller gesetzt werden
		public bool IsLocked { get; set; }

		public TopicLock Lock { get; set; }

		public virtual ICollection<UnreadState> UnreadBy { get; set; }

		public virtual ICollection<TopicVisibilityOverride> VisibilityOverrides { get; set; }
		
		public bool IsUnreadBy(int userID)
		{
			return UnreadBy.Any(x => x.UserID == userID);
		}

		public bool HasDecision()
		{
			return Decision != null;
		}

		public bool HasDecision(DecisionType d)
		{
			return Decision != null && Decision.Type == d;
		}

		internal AuthResult IsEditableBy(User u, ActiveSession s)
		{
			if (IsReadOnly)
				return new AuthResult("Dieser Diskussionspunkt ist nicht bearbeitbar.");
			if (Lock != null)
			{
				// Von einer Sitzung gesperrt
				if (u.Equals(Lock.Session.Manager)) // Der Punkt ist in einer Sitzung, aber der aktuelle Benutzer ist der Sitzungsleiter
					return new AuthResult(true);

				return new AuthResult("Dieser Diskussionspunkt ist gesperrt, und nur durch den Sitzungsleiter bearbeitbar.");
			}

			// Besitzer darf bearbeiten
			if (u.Equals(Owner))
				return new AuthResult(true);

			if (s == null)
				return new AuthResult("Sie können diesen Diskussionspunkt nicht bearbeiten, da sie nicht der Besitzer sind.");
			if (s.SessionType.ID != SessionTypeID)
			{
				return
					new AuthResult("Sie können diesen Diskussionspunkt nicht bearbeiten, da der Punkt nicht in ihre Sitzung fällt.");
			}

			// Lock == null && s != null && s.SessionType.ID == SessionTypeID
			// Der aktuelle Benutzer ist zwar Sitzungsleiter einer Sitzung, in die dieser Punkt fallen würde.
			// Er wurde aber beim erstellen der Sitzung nicht erfasst, z.B. weil er nach Sitzungsbeginn erstellt wurde.
			return new AuthResult("Dieser Punkt ist in ihrer Sitzung nicht enthalten.");
		}

		public void IncorporateUpdates(TopicEdit updates)
		{
			if (IsReadOnly)
				throw new InvalidOperationException("Diese Diskussion ist beendet und kann daher nicht bearbeitet werden.");

			Description = updates.Description;
			OwnerID = updates.OwnerID;
			Priority = updates.Priority;
			Proposal = updates.Proposal;
			ResubmissionDate = updates.ResubmissionDate;
			Title = updates.Title;
			Time = updates.Time;
			ValidFrom = DateTime.Now;
		}

		[Display(Name = "Rechteverwaltung")]
		public int? AclID { get; set; }

		public virtual ACL Acl { get; set; }
	}

	internal class TopicByIdComparer : IEqualityComparer<Topic>
	{
		public bool Equals(Topic x, Topic y)
		{
			return x.ID == y.ID;
		}

		public int GetHashCode(Topic obj)
		{
			return obj.ID;
		}
	}

	internal struct AuthResult
	{
		public readonly bool IsAuthorized;
		public readonly string Reason;

		public AuthResult(string reason)
		{
			IsAuthorized = false;
			Reason = reason;
		}

		public AuthResult(bool isAuthorized)
		{
			IsAuthorized = isAuthorized;
			Reason = "";
		}
	}

	[Serializable]
	public sealed class TopicLockedException : Exception
	{
		public TopicLockedException()
			: base("Das Thema ist gesperrt und kann daher nur durch den Sitzungsleiter bearbeitet werden.")
		{
		}

		public TopicLockedException(string message)
			: base(message)
		{
		}
	}
}