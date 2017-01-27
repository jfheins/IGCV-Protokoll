﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Principal;
using System.Web.Mvc;
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

			using (var context = new PrincipalContext(ContextType.Domain, DomainName))
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
				var actionResult = ImportNewUsers(context, myusers);
				if (actionResult != null)
					return actionResult;

				actionResult = ImportNewGroupMembers(context, myusers);
				if (actionResult != null)
					return actionResult;
			}
			db.SaveChanges();

			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}

		private ActionResult ImportNewGroupMembers(PrincipalContext context, List<User> myusers)
		{
			foreach (var group in _authorizeGroups)
			{
				using (GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, @group))
				{
					if (groupPrincipal == null)
					{
						return HTTPStatus(HttpStatusCode.InternalServerError, $"Die Gruppe \"{@group}\" wurde nicht gefunden.");
					}

					foreach (var adUser in groupPrincipal.GetMembers(true).OfType<UserPrincipal>())
					{
						User myUser = myusers.SingleOrDefault(u => u.Guid == adUser.Guid);
						if (myUser == null)
						{
							myUser = CreateUserFromADUser(adUser);
							db.Users.Add(myUser);
						}
						myUser.IsActive = true;
					}
				}
			}
			return null;
		}

		private ActionResult ImportNewUsers(PrincipalContext context, IEnumerable<User> myusers)
		{
			foreach (var userName in _authorizeUsers)
			{
				using (UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userName))
				{
					if (userPrincipal == null)
					{
						return HTTPStatus(HttpStatusCode.InternalServerError, $"Der Benutzer \"{userName}\" wurde nicht gefunden.");
					}
					User user = myusers.SingleOrDefault(u => u.Guid == userPrincipal.Guid);
					if (user == null)
					{
						user = CreateUserFromADUser(userPrincipal);
						db.Users.Add(user);
					}
					user.IsActive = true;
				}
			}
			return null;
		}

		private static void SyncUsersByShortName(IEnumerable<User> myusers, Dictionary<Guid, UserPrincipal> employees)
		{
			foreach (User user in myusers.Where(u => u.Guid == Guid.Empty))
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

			using (var context = new PrincipalContext(ContextType.Domain, DomainName))
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
			using (var context = new PrincipalContext(ContextType.Domain, DomainName))
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
	}
}