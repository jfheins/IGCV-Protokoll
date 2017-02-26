using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Administration.Models;
using IGCV_Protokoll.Areas.Session.Models;
using IGCV_Protokoll.Areas.Session.Models.Lists;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.util;
using IGCV_Protokoll.ViewModels;
using JetBrains.Annotations;

namespace IGCV_Protokoll.DataLayer
{
	public class DataContext : DbContext
	{
		public DataContext()
			: base("DataContext")
		{
		}

		public DbSet<ACL> ACLs { get; set; }
		public DbSet<ACLItem> ACLItems { get; set; }
		public DbSet<ActiveAgendaItem> ActiveAgendaItems { get; set; }
		public DbSet<ActiveSession> ActiveSessions { get; set; }
		public DbSet<AdEntity> AdEntities { get; set; }
		public DbSet<AdEntityUser> AdEntityUsers { get; set; }
		public DbSet<AgendaItem> AgendaItems { get; set; }
		public DbSet<AgendaTemplate> AgendaTemplates { get; set; }
		public DbSet<Assignment> Assignments { get; set; }
		public DbSet<Comment> Comments { get; set; }
		public DbSet<Decision> Decisions { get; set; }
		public DbSet<Document> Documents { get; set; }
		public DbSet<DocumentContainer> DocumentContainers { get; set; }
		public DbSet<Conference> LConferences { get; set; }
		public DbSet<EmployeePresentation> LEmployeePresentations { get; set; }
		public DbSet<Event> LEvents { get; set; }
		public DbSet<Extension> LExtensions { get; set; }
		public DbSet<IlkDay> LIlkDays { get; set; }
		public DbSet<IlkMeeting> LIlkMeetings { get; set; }
		public DbSet<IndustryProject> LIndustryProject { get; set; }
		public DbSet<Holiday> LHolidays { get; set; }
		public DbSet<Opening> LOpenings { get; set; }
		public DbSet<ResearchProposal> LResearchProposal { get; set; }
		public DbSet<PushNotification> PushNotifications { get; set; }
		public DbSet<Revision> Revisions { get; set; }
		public DbSet<SessionReport> SessionReports { get; set; }
		public DbSet<SessionType> SessionTypes { get; set; }
		public DbSet<Tag> Tags { get; set; }
		public DbSet<TagTopic> TagTopics { get; set; }
		public DbSet<Topic> Topics { get; set; }
		public DbSet<TopicHistory> TopicHistory { get; set; }
		public DbSet<TopicLock> TopicLocks { get; set; }
		public DbSet<UnreadState> UnreadState { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<Vote> Votes { get; set; }

		public IQueryable<Topic> FilteredTopics(int[] userRoles)
		{
			return Topics.Where(t => t.Acl == null || t.Acl.Items.Select(i => i.AdEntityID).Any(x => userRoles.Contains(x)));
		}
		public IQueryable<Decision> FilteredDecisions(int[] userRoles)
		{
			return FilteredTopics(userRoles).Where(t => t.Decision != null).Select(t => t.Decision);
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

			modelBuilder.Entity<EmployeePresentation>()
			.HasRequired(x => x.Documents);
		}

		/// <summary>
		///    Gibt alle Benutzer zurück, mit dem aktuellen Benutzer als erstes Element und den restlichen Benutzern in
		///    alphabetischer Reihenfolge.
		/// </summary>
		/// <param name="currentUser">Der aktuelle Benutzer, dieser wird an den Anfang der Liste sortiert.</param>
		/// <returns></returns>
		public IEnumerable<User> GetUserOrdered([CanBeNull] User currentUser)
		{
			if (currentUser != null)
			{
				return
					currentUser.ToEnumerable()
						.Concat(Users
							.Where(u => u.ID != currentUser.ID && u.IsActive)
							.ToList()
							.OrderBy(u => u.ShortName, StringComparer.CurrentCultureIgnoreCase));
			}
			else
			{
				return Users
					.Where(u => u.IsActive)
					.ToList()
					.OrderBy(u => u.ShortName, StringComparer.CurrentCultureIgnoreCase);
			}
		}

		public IQueryable<User> GetActiveUsers()
		{
			return Users.Where(u => u.IsActive);
		}

		/// <summary>
		/// Löscht ein Thema permanent aus der Datenbank.
		/// </summary>
		/// <param name="topicID">Die ID des zu löschenden Themas.</param>
		public void DeleteTopic(int topicID)
		{
			var topic = Topics.Find(topicID);
			if (topic == null)
				return;

			// Dokumente bleiben erhalten, der Dokumentencontainer ist dann allerdings verwaist.
			topic.Documents.Topic = null;
			topic.Documents.Orphaned = DateTime.Now;
			SaveChanges();

			Votes.RemoveRange(Votes.Where(v => v.Topic.ID == topicID));
			Topics.Remove(topic);
			SaveChanges();
		}

		public IQueryable<ACLItem> GetACL([NotNull] IAccessible obj)
		{
			return obj.AclID == null ? null : ACLItems.Include(i => i.AdEntity).Where(item => item.ParentId == obj.AclID);
		}

		public int[] GetRolesForUser(int userid)
		{
			return AdEntityUsers.Where(item => item.UserID == userid).Select(item => item.AdEntityID).Distinct().ToArray();
		}
	}
}