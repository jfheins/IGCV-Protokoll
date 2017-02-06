using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Principal;
using System.Web.Mvc;
using EntityFramework.Extensions;
using IGCV_Protokoll.Controllers;
using IGCV_Protokoll.DataLayer;
using IGCV_Protokoll.Mailers;
using IGCV_Protokoll.Models;
using JetBrains.Annotations;
using StackExchange.Profiling;

namespace IGCV_Protokoll.Areas.Administration.Controllers
{
	/// <summary>
	///    Der UserController umfasst alle Funktionen der Benutzerverwaltung.
	/// </summary>
	[DisplayName("Benutzer")]
	public class UserController : BaseController
	{
		public const string DomainName = "IGCV";
		// Benutzer dieser Gruppen werden automatisch hinzugefügt
		private readonly string[] _authorizeGroups = { "V-AL", "Protokoll-Developer" };
		private readonly string[] _authorizeUsers = { "Schilpjo", "Reinhart" };
		private readonly string _rootGroup = "V-MA";

		/// <summary>
		///    Wird aufgerufen, bevor die Aktionsmethode aufgerufen wird.
		/// </summary>
		/// <param name="filterContext">Informationen über die aktuelle Anforderung und Aktion.</param>
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.AdminStyle = "active";
			ViewBag.AUserStyle = "active";
		}

		protected static PrincipalContext CreateContext()
		{
			return new PrincipalContext(ContextType.Domain, DomainName);
		}

		// GET: Administration/User
		/// <summary>
		///    Die Benutzerübersicht, es können keine Bearbeitungen vorgenommen werden.
		/// </summary>
		public ActionResult Index()
		{
			List<User> users;
			using (MiniProfiler.Current.Step("DB-Abfrage"))
			{
				users = db.Users.OrderByDescending(u => u.IsActive).ThenBy(u => u.ShortName).ToList();
			}
			return View("Index", users);
		}

		// AJAX: Administration/User/Sync
		/// <summary>
		///    Hier werden die Benutzer des IGCV-Protokolls abgeglichen. Das heißt, Benutzer die Autorisiert sind, werden in die
		///    Datenbank aufgenommen und können demzufolge in den Auswahllisten ausgewählt werden. Es findet keine Autorisirung
		///    statt; diese erfolgt bereits früher in Form des globelen Filters mit dem AuthorizeAttribute in FilterConfig.cs.
		///    Falls Benutzer nicht mehr in den autorisierten Gruppen sind, werden Sie inaktiv geschaltet. Das bedeutet, dass sie
		///    nicht mehr in den Listen auftauchen und auch keine E-Mails mehr bekommen.
		/// </summary>
		public ActionResult _Sync()
		{
			List<User> myusers = db.Users.ToList();
			// Zunächst alle Benutzer (außer dem aktuellen Benutzer) auf inaktiv setzen.
			foreach (User user in myusers)
				user.IsActive = user.Equals(GetCurrentUser());

			using (var context = CreateContext())
			using (var userp = new UserPrincipal(context))
			using (var searcher = new PrincipalSearcher(userp))
			{
				PrincipalSearchResult<Principal> adEmployees = searcher.FindAll();

				Dictionary<Guid, UserPrincipal> employees =
					adEmployees.Where(p => p.Guid != null).Cast<UserPrincipal>().ToDictionary(p => p.Guid.Value);


				// Benutzer, zu denen eine GUID gespeichert ist, werden zuerst synchronisiert, da die Übereinstimmung garantiert richtig ist
				SyncUsersByGuid(myusers, employees);

				// Als zweites wird über das Namenskürzel synchronisiert. Die User ohne GUID bekommen hier eine GUID.
				SyncUsersByShortName(myusers, employees);

				// Schließlich werden neue User in die Datenbank importiert.
				try
				{
					foreach (var authorizedUser in EnumerateAuthorizedUsers(context))
					{
						var myUser = myusers.SingleOrDefault(u => u.Guid == authorizedUser.Guid);
						if (myUser != null)
							myUser.IsActive = true;
						else
							db.Users.Add(CreateUserFromADUser(authorizedUser));
					}
				}
				catch (InvalidConfigurationException ex)
				{
					return HTTPStatus(HttpStatusCode.InternalServerError, ex.Message);
				}
			}
			db.SaveChanges();

			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}

		private IEnumerable<UserPrincipal> EnumerateAuthorizedUsers(PrincipalContext context)
		{
			foreach (var userName in _authorizeUsers)
			{
				using (UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userName))
				{
					if (userPrincipal == null)
						throw new InvalidConfigurationException($"Der Benutzer \"{userName}\" wurde nicht gefunden.");

					yield return userPrincipal;
				}
			}
			foreach (var group in _authorizeGroups)
			{
				using (GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, @group))
				{
					if (groupPrincipal == null)
						throw new InvalidConfigurationException($"Die Gruppe \"{@group}\" wurde nicht gefunden.");

					foreach (var adUser in groupPrincipal.GetMembers(true).OfType<UserPrincipal>())
					{
						yield return adUser;
					}
				}
			}
		}

		private static void SyncUsersByShortName(IEnumerable<User> myusers, Dictionary<Guid, UserPrincipal> employees)
		{
			foreach (var user in myusers.Where(u => u.Guid == Guid.Empty))
			{
				UserPrincipal adUser = employees.Values.SingleOrDefault(u => u.SamAccountName == user.ShortName);
				if (adUser != null && adUser.Guid != null)
				{
					user.Guid = adUser.Guid.Value;
					user.LongName = adUser.DisplayName;
					user.EmailAddress = adUser.EmailAddress;
					employees.Remove(user.Guid);
				}
			}
		}

		private static void SyncUsersByGuid(IEnumerable<User> myusers, Dictionary<Guid, UserPrincipal> employees)
		{
			foreach (User user in myusers.Where(u => u.Guid != Guid.Empty))
			{
				UserPrincipal adUser;
				if (employees.TryGetValue(user.Guid, out adUser))
				{
					user.ShortName = adUser.SamAccountName;
					user.LongName = adUser.DisplayName;
					if (adUser.EmailAddress != null)
						user.EmailAddress = adUser.EmailAddress;
					employees.Remove(user.Guid);
				}
			}
		}

		/// <summary>
		///    Liefert anhand des angegebenen IPrincipal einen Benutzer zurück. Wenn der Benutzer in der Datenbank enthalten ist,
		///    wird dieser zurückgeliefert. Ist er nicht in der Datenbank, wird er angelegt. Ist der Benutzername leer, wird ein
		///    anonymer Benutzer namens "xx" zurückgegeben, der nicht in der Datenbank enthalten ist.
		/// </summary>
		/// <param name="db">Ein Datenkontext</param>
		/// <param name="userPrincipal">Der gesuchte Benutzer</param>
		/// <returns></returns>
		[NotNull]
		public static User GetUser(DataContext db, IPrincipal userPrincipal)
		{
			string fullName = userPrincipal.Identity.Name;
			string shortName = fullName.Split('\\').Last();

			if (string.IsNullOrEmpty(fullName))
				return new User { ID = 0, ShortName = "xx", LongName = "Anonymous User" };
#if DEBUG
			if (fullName == @"JULIUS-DESKTOP\Julius")
				return db.Users.Find(1);
#endif

			using (var context = CreateContext())
			using (UserPrincipal aduser = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, fullName))
			{
				if (aduser == null || aduser.Guid == null)
					throw new AuthenticationException("Keine GUID im AD gefunden.");

				User user = db.Users.FirstOrDefault(u => u.Guid == aduser.Guid.Value);
				if (user != null)
				{
					if (!user.IsActive)
					{
						user.IsActive = true;
						db.SaveChanges();
					}
					return user;
				}
				else
				{
					user = db.Users.SingleOrDefault(u => u.ShortName.Equals(shortName, StringComparison.CurrentCultureIgnoreCase));
					if (user == null)
					{
						user = CreateUserFromADUser(aduser);
						db.Users.Add(user);
					}
					else
					{
						user.Guid = aduser.Guid.Value;
						user.LongName = aduser.DisplayName;
						user.EmailAddress = aduser.EmailAddress;
						user.IsActive = true;
					}
					db.SaveChanges();
					return user;
				}
			}
		}

		/// <summary>
		///    Erzeugt einen neuen Benutzer in der Datenbank anhand eines Namenskürzels. Wird momentan nicht verwendet.
		/// </summary>
		/// <param name="samname">Das Namenskürzel, typischerweise 2 oder 3 Zeichen.</param>
		/// <returns>Einen neuen Benutzer, dessen Daten anhand des Kürzels aus dem AD gezogen wurden.</returns>
		public static User CreateUserFromShortName(string samname)
		{
			using (var context = CreateContext())
			using (UserPrincipal aduser = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samname))
			{
				if (aduser == null || aduser.Guid == null)
					throw new AuthenticationException("Keine GUID im AD gefunden.");

				return new User
				{
					Guid = aduser.Guid.Value,
					ShortName = aduser.SamAccountName,
					LongName = aduser.DisplayName,
					EmailAddress = aduser.EmailAddress,
					IsActive = true
				};
			}
		}

		/// <summary>
		///    Erzeugt einen neuen Benutzer in der Datenbank anhand eines UserPrincipal. Alle Daten des Benutzers werden aus dem AD
		///    gezogen, außerdem bekommt der neue Benutzer eine Willkommens-E-Mail
		/// </summary>
		/// <param name="aduser">Der UserPrincipal, aus dessen Daten der Benutzer erzeugt werden soll.</param>
		private static User CreateUserFromADUser(UserPrincipal aduser)
		{
			var u = new User
			{
				Guid = aduser.Guid ?? Guid.Empty,
				ShortName = aduser.SamAccountName,
				LongName = aduser.DisplayName,
				EmailAddress = aduser.EmailAddress,
				IsActive = true
			};
			new UserMailer().SendWelcome(u);
			return u;
		}

		public ActionResult PullADEntities()
		{
			// Value gibt an, ob eine Synchronisierung stattfand
			var existingEntities = db.AdEntities.ToDictionary(x => x.Guid);
			var touchedEntities = new HashSet<Guid>();

			using (var context = CreateContext())
			{
				using (var root = GroupPrincipal.FindByIdentity(context, IdentityType.Name, _rootGroup))
				{
					if (root?.Guid == null)
						throw new InvalidConfigurationException($"Zu dem Wurzelelement {_rootGroup} wurde keine GUID gefunden!");

					touchedEntities.Add(root.Guid.Value);
					SyncEntity(root, null, existingEntities, touchedEntities);
				}
			}
			// Neue Datensätze speichern
			db.SaveChanges();

			var forRemoval = db.AdEntities.Where(dbEntry => !touchedEntities.Contains(dbEntry.Guid)).ToArray();
			db.AdEntities.RemoveRange(forRemoval);
			db.SaveChanges();
			
			// Zuordnungen User-Entity synchronisieren
			var rootEntity = db.AdEntities.Single(e => e.ParentID == null);
			SyncMembership(rootEntity, new Stack<AdEntity>());
			db.SaveChanges();

			return Content(touchedEntities.Count.ToString());
		}

		/// <summary>
		/// Synchronisiert rekursiv ab der Wurzel die Hierarchie des AD. Gibt die ADEntity zurück.
		/// </summary>
		private void SyncEntity(Principal source, AdEntity parent, Dictionary<Guid, AdEntity> map, HashSet<Guid> touched)
		{
			if (source?.Guid == null || source.Guid == Guid.Empty)
				return;

			AdEntity newEntity;
			if (map.TryGetValue(source.Guid.Value, out newEntity))
			{
				// Update eines vorhandenen Datensatzes
				newEntity.Parent = parent;
				newEntity.Name = source.Name;
				newEntity.SamAccountName = source.SamAccountName;
				newEntity.SetTypeByPrincipal(source);
			}
			else
			{
				// Neuer Datensatz
				newEntity = db.AdEntities.Add(new AdEntity(source)
				{
					Parent = parent,
					Name = source.Name,
					SamAccountName = source.SamAccountName,
					Guid = source.Guid.Value
				});
			}
			touched.Add(newEntity.Guid);

			if (!(source is GroupPrincipal))
				return;

			foreach (var member in ((GroupPrincipal)source).GetMembers(false))
			{
				SyncEntity(member, newEntity, map, touched);
			}
		}

		private void SyncMembership(AdEntity item, Stack<AdEntity> parentStack)
		{
			if (item.Children.Any())
			{
				parentStack.Push(item);
				foreach (var child in item.Children)
				{
					SyncMembership(child, parentStack);
				}
				parentStack.Pop();
			}
			else
			{
				// User ==> Mitgliedschaften aktualisieren
				var user = db.Users.FirstOrDefault(u => u.Guid == item.Guid);
				if (user != null)
				{
					var newMemberships = parentStack.ToArray();
					var oldMemberships = user.AdGroups.Select(x => x.AdEntity).ToArray();
					var toRemove = oldMemberships.Except(newMemberships).Select(x => x.ID).ToArray();
					if (toRemove.Length > 0)
					{
						db.AdEntityUsers.Where(x => x.UserID == user.ID && toRemove.Contains(x.AdEntityID)).Delete();
					}
					var toAdd = newMemberships.Except(oldMemberships);
					db.AdEntityUsers.AddRange(toAdd.Select(x => new AdEntityUser {AdEntity = x, User = user}));
				}
			}
		}
	}


	[System.Serializable]
	public class InvalidConfigurationException : Exception
	{
		public InvalidConfigurationException() { }
		public InvalidConfigurationException(string message) : base(message) { }
		public InvalidConfigurationException(string message, Exception inner) : base(message, inner) { }
	}
}