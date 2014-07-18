﻿using System.Data.Entity.Validation;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ILK_Protokoll.Areas.Administration.Controllers;
using ILK_Protokoll.Areas.Session.Models;
using ILK_Protokoll.DataLayer;
using ILK_Protokoll.Models;

namespace ILK_Protokoll.Controllers
{
	public class BaseController : Controller
	{
		protected readonly DataContext db = new DataContext();

		protected User _CurrentUser;

		protected User GetCurrentUser()
		{
			if (_CurrentUser == null)
			{
				if (Session["UserID"] == null)
				{
					_CurrentUser = UserController.GetUser(db, User);
					Session["UserID"] = _CurrentUser.ID;
				}
				else
					_CurrentUser = db.Users.Find(Session["UserID"]);
			}

			return _CurrentUser;
		}

		protected ActiveSession GetSession()
		{
			var sessionID = (int?)Session["SessionID"];
			if (sessionID.HasValue && sessionID > 0)
				return db.ActiveSessions.Find(sessionID);
			else
				return null;
		}

		protected ContentResult HTTPStatus(int statuscode, string message)
		{
			Response.Clear();
			Response.StatusCode = statuscode;

			if (statuscode == 422)
				Response.StatusDescription = "Unprocessable Entity";
			else if (statuscode == 403)
				Response.StatusDescription = "Forbidden";
			else
				Response.StatusDescription = "Internal Server Error";

			return Content(message);
		}

		protected string ErrorMessageFromException(DbEntityValidationException ex)
		{
			StringBuilder msg = new StringBuilder();
			foreach (var entity in ex.EntityValidationErrors)
				foreach (var error in entity.ValidationErrors)
					msg.AppendFormat("{0}: {1} <br />", error.PropertyName, error.ErrorMessage);

			return msg.ToString();
		}

		protected string ErrorMessageFromModelState()
		{
			StringBuilder msg = new StringBuilder();
			foreach (var kvp in ModelState)
				foreach (var error in kvp.Value.Errors)
					msg.AppendFormat("{0}: {1} <br />", kvp.Key, error.ErrorMessage);

			return msg.ToString();
		}

		protected override void Initialize(RequestContext requestContext)
		{
			HttpContextBase Context = requestContext.HttpContext;

			Context.Response.SuppressDefaultCacheControlHeader = true;

			Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
			Context.Response.Headers["Pragma"] = "no-cache";
			Context.Response.Headers["Expires"] = "0";

			base.Initialize(requestContext);
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.ColorScheme = GetCurrentUser().ColorScheme;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				db.Dispose();

			base.Dispose(disposing);
		}
	}
}