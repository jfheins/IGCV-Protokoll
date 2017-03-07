using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using EntityFramework.Extensions;

namespace IGCV_Protokoll.Controllers
{
    public class DocumentContainerController : BaseController
    {
        // GET: DocumentContainer
        public ActionResult Index()
        {
            return View();
        }

	    public ActionResult _PermanentDelete(int containerID)
	    {
		    var container = db.DocumentContainers.Find(containerID);

		    if (container == null)
			    return HttpNotFound();
			if (!IsAuthorizedFor(container))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Container nicht berechtigt!");
			
		    foreach (var document in container.Documents)
		    {
			    var errorMsg = _PermanentDeleteDocument(document.ID);
			    if (errorMsg != null)
				    return HTTPStatus(HttpStatusCode.InternalServerError, errorMsg);
		    }

		    db.DocumentContainers.Where(dc => dc.ID == containerID).Delete();
			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}

		private string _PermanentDeleteDocument(int documentID)
		{
			var document = db.Documents.Include(d => d.LatestRevision).Single(d => d.ID == documentID);
			
			try
			{
				foreach (var revision in document.Revisions)
				{
					string path = Path.Combine(AttachmentsController.Serverpath, revision.FileName);
					System.IO.File.Delete(path);
				}
			}
			catch (IOException ex)
			{
				return ex.Message;
			}

			try
			{
				document.LatestRevisionID = null;
				db.Revisions.RemoveRange(document.Revisions);
				db.SaveChanges();
				db.Documents.Remove(document);
				db.SaveChanges();
			}
			catch (DbEntityValidationException e)
			{
				var message = ErrorMessageFromException(e);
				return message;
			}

			return null;
		}
	}
}