using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using EntityFramework.Extensions;
using IGCV_Protokoll.DataLayer;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.util;
using JetBrains.Annotations;

namespace IGCV_Protokoll.Controllers
{
	public class AttachmentsController : BaseController
	{
		private static readonly Regex InvalidChars = new Regex(@"[^a-zA-Z0-9_-]");

		/// <summary>
		///    Diese Erweiterungen sind charakteristisch für MS-Office Dateien. Wird die Seite im Internet-Explorer genutzt, werden
		///    diese Erweiterungen zum direkten Öffnen angeboten.
		/// </summary>
		private static readonly HashSet<string> OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"doc",
			"docm",
			"docx",
			"dotx",
			"one",
			"pdf",
			"potx",
			"ppt",
			"pptx",
			"vsd",
			"xls",
			"xlsm",
			"xlsx",
			"xltx"
		};

		/// <summary>
		/// Enthält die bekannten Dateitypen, denen ein Icon zugeordnet werden kann. Die Dateierweiterung ist der Schlüssel,
		/// die genaue Dateiname des Icons ist der Schlüssel. (Nur relevant auf Dateisystemen, die case-sensitiv sind)
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
		public static Dictionary<string, string> KnownExtensions = new Dictionary<string, string>();

		public static string Serverpath => ConfigurationManager.AppSettings["UploadPath"];

		private static string TemporaryServerpath => ConfigurationManager.AppSettings["TempPath"];

		private static bool EnableSeamlessEditing => Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSeamlessEditing"]);

		private static bool EnableSeamlessDownload => Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSeamlessDownload"]);

#if DEBUG
		private readonly string _hostname = Dns.GetHostName();
#else
		private readonly string _hostname = Dns.GetHostName() + ".igcv.fraunhofer.de";
#endif

		private bool isInternetExplorer
		{
			get
			{
				var userAgent = Request.UserAgent;
				return !string.IsNullOrEmpty(userAgent) && userAgent.Contains("Trident");
			}
		}

		// GET: Attachments
		public ActionResult _List(int id, bool makeList = false, bool showActions = true) // id = DocumentContainer.ID
		{
			var container = db.DocumentContainers.Find(id);
			if (container == null)
				return HTTPStatus(HttpStatusCode.NotFound, "Container nicht gefunden");
			if (!IsAuthorizedFor(container))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diese Dokumente nicht berechtigt!");

			var documents = db.Documents
				.Where(a => a.Deleted == null)
				.Where(d => d.ParentContainerID == id)
				.OrderBy(a => a.DisplayName)
				.Include(a => a.Revisions)
				.Include(a => a.LatestRevision)
				.Include(a => a.LatestRevision.Uploader);

			KnownExtensions = (from path in Directory.GetFiles(Server.MapPath("~/img/fileicons"), "*.png")
							   select Path.GetFileNameWithoutExtension(path)).ToDictionary(x => x, StringComparer.OrdinalIgnoreCase);

			ViewBag.ContainerID = id;
			ViewBag.KnownExtensions = KnownExtensions;
			ViewBag.OfficeExtensions = OfficeExtensions;
			ViewBag.SeamlessEnabled = EnableSeamlessEditing && isInternetExplorer;

			if (makeList)
				return PartialView("_AttachmentList", documents.ToList());
			else
			{
				return showActions
					? PartialView("_AttachmentTable", documents.ToList())
					: PartialView("~/Areas/Session/Views/Finalize/_ReportAttachments.cshtml", documents.ToList());
			}
		}

		public ActionResult ContainerDetails(int id, string returnURL) // id = DocumentContainer.ID
		{
			var container = db.DocumentContainers.Include(dc => dc.Topic).SingleOrDefault(dc => dc.ID == id);
			if (container == null)
				return HTTPStatus(HttpStatusCode.NotFound, "Container nicht gefunden");
			if (!IsAuthorizedFor(container))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diese Dokumente nicht berechtigt!");

			ViewBag.ShowUpload = true;
			var topic = container.Topic;
			if (topic != null)
				ViewBag.ShowUpload = !topic.IsReadOnly && !IsTopicLocked(topic.ID);

			ViewBag.ReturnURL = returnURL;

			return View(container);
		}

		public ActionResult Details(int id) // id = Document.ID
		{
			var document = db.Documents.Include(d => d.LockUser).Include(d => d.ParentContainer).SingleOrDefault(doc => doc.ID == id);
			if (document == null)
				return HTTPStatus(HttpStatusCode.NotFound, "Datei nicht gefunden");
			if (!IsAuthorizedFor(document.ParentContainer))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für dieses Dokument nicht berechtigt!");

			ViewBag.ShowUpload = document.LockTime == null;
			var topic = document.ParentContainer.Topic;
			if (ViewBag.ShowUpload && topic != null)
				ViewBag.ShowUpload = !topic.IsReadOnly && !IsTopicLocked(topic.ID);

			ViewBag.SeamlessEnabled = EnableSeamlessEditing && isInternetExplorer && OfficeExtensions.Contains(document.LatestRevision.Extension);

			if (document.LockUserID == GetCurrentUserID())
				ViewBag.TempFileURL = "file://" + _hostname + "/Temp/" + document.Revisions.OrderByDescending(r => r.Created).First().FileName;

			return View(document);
		}

		public ActionResult _UploadForm(int id) // id = DocumentContainer.ID
		{
			var container = db.DocumentContainers.Find(id);
			if (container == null)
				return HttpNotFound();
			if (!IsAuthorizedFor(container))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diese Dokumente nicht berechtigt!");

			if (container.TopicID.HasValue && IsTopicLocked(container.TopicID.Value))
				return Content("<div class=\"panel-footer\">Da das Thema gesperrt ist, können Sie keine Dateien hochladen.</div>");
			else
				return PartialView("_DocumentCreateDropZone", container);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult _CreateDocuments(int id) // id = DocumentContainer.ID
		{
			if (Request.Files.Count == 0)
				return HTTPStatus(HttpStatusCode.BadRequest, "Es wurden keine Dateien empfangen.");

			var container = db.DocumentContainers.Find(id);
			if (container == null)
				return HTTPStatus(HttpStatusCode.BadRequest, "Die Dateien können keinem Ziel zugeordnet werden.");
			if (!IsAuthorizedFor(container))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Container nicht berechtigt!");

			Topic topic = container.Topic;
			if (topic != null)
			{
				if (IsTopicLocked(topic.ID))
					return HTTPStatus(HttpStatusCode.Forbidden, "Da das Thema gesperrt ist, können Sie keine Dateien hochladen.");

				if (topic.IsReadOnly)
				{
					return HTTPStatus(HttpStatusCode.Forbidden,
						"Da das Thema schreibgeschützt ist, können Sie keine Dateien bearbeiten.");
				}
			}

			var statusMessage = new StringBuilder();
			int successful = 0;

			for (int i = 0; i < Request.Files.Count; i++)
			{
				HttpPostedFileBase file = Request.Files[i];

				if (file == null)
					continue;

				if (string.IsNullOrWhiteSpace(file.FileName))
				{
					statusMessage.AppendLine("Eine Datei hat einen ungültigen Dateinamen.");
					continue;
				}
				string fullName = Path.GetFileName(file.FileName);
				if (file.ContentLength == 0)
				{
					statusMessage.AppendFormat("Datei \"{0}\" hat keinen Inhalt.", fullName).AppendLine();
					continue;
				}

				string filename = Path.GetFileNameWithoutExtension(file.FileName);
				string fileext = Path.GetExtension(file.FileName);
				if (!string.IsNullOrEmpty(fileext))
					fileext = fileext.Substring(1);

				var revision = new Revision
				{
					SafeName = InvalidChars.Replace(filename, "").Truncate(100),
					FileSize = file.ContentLength,
					UploaderID = GetCurrentUserID(),
					Extension = fileext,
					GUID = Guid.NewGuid(),
					Created = DateTime.Now
				};

				Document document = new Document(revision)
				{
					Deleted = null,
					DisplayName = fullName,
					ParentContainer = container
				};

				try
				{
					db.Documents.Add(document);
					db.SaveChanges(); // Damit die Revision die ID bekommt. Diese wird anschließend im Dateinamen hinterlegt
					document.LatestRevision = revision;
					db.SaveChanges();
					string path = Path.Combine(Serverpath, revision.FileName);
					file.SaveAs(path);
					successful++;
				}
				catch (DbEntityValidationException ex)
				{
					var message = ErrorMessageFromException(ex);
					statusMessage.AppendFormat("Datei \"{0}\" konnte nicht in der Datenbank gespeichert werden.\n{1}", fullName,
						message)
						.AppendLine();
				}
				catch (IOException)
				{
					statusMessage.AppendFormat("Datei \"{0}\" konnte nicht gespeichert werden.", fullName).AppendLine();
				}
			}
			statusMessage.AppendFormat(
				successful == 1 ? "Eine Datei wurde erfolgreich verarbeitet." : "{0} Dateien wurden erfolgreich verarbeitet.",
				successful);

			// Ungelesen-Markierung aktualisieren
			if (topic != null && successful > 0)
				MarkAsUnread(topic);

			ViewBag.StatusMessage = statusMessage.ToString();

			return _List(id);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult CreateNewRevision(int id) // id = Document.ID
		{
			if (Request.Files.Count != 1)
				return HTTPStatus(HttpStatusCode.BadRequest, "Keine Datei empfangen");

			var file = Request.Files[0];
			if (file == null)
				return HTTPStatus(HttpStatusCode.BadRequest, "Keine Datei empfangen");

			// Checks
			Document document;
			Topic topic;
			var actionResult = CheckConstraints(id, out topic, out document);
			if (actionResult != null)
				return actionResult;
			//---------------------------------------------------------------

			if (string.IsNullOrWhiteSpace(file.FileName))
				return HTTPStatus(HttpStatusCode.BadRequest, "Die Datei hat einen ungültigen Dateinamen.");

			var fullName = Path.GetFileName(file.FileName);
			if (file.ContentLength == 0)
				return HTTPStatus(HttpStatusCode.BadRequest, "Datei " + fullName + " hat keinen Inhalt.");

			string filename = Path.GetFileNameWithoutExtension(file.FileName);
			string fileext = Path.GetExtension(file.FileName);
			if (!string.IsNullOrEmpty(fileext))
				fileext = fileext.Substring(1);

			var revision = new Revision
			{
				ParentDocument = document,
				SafeName = InvalidChars.Replace(filename, "").Truncate(100),
				FileSize = file.ContentLength,
				UploaderID = GetCurrentUserID(),
				Extension = fileext,
				GUID = Guid.NewGuid(),
				Created = DateTime.Now
			};
			document.Revisions.Add(revision);
			document.LatestRevision = revision;
			document.DisplayName = fullName;

			try
			{
				db.SaveChanges(); // Damit die Revision seine ID bekommt. Diese wird anschließend im Dateinamen hinterlegt
				string path = Path.Combine(Serverpath, revision.FileName);
				file.SaveAs(path);
			}
			catch (DbEntityValidationException ex)
			{
				return HTTPStatus(HttpStatusCode.InternalServerError, ErrorMessageFromException(ex));
			}
			catch (IOException ex)
			{
				return HTTPStatus(HttpStatusCode.InternalServerError, "Dateisystemfehler: " + ex.Message);
			}

			// Ungelesen-Markierung aktualisieren
			if (topic != null)
				MarkAsUnread(topic);

			return HTTPStatus(HttpStatusCode.Created, Url.Action("Details", "Attachments", new { Area = "", id }));
		}

		/// <summary>
		///    Prüft verschiedene Kriterien, nach denen das Dokument bearbeitbar ist. Bei Einem Fehler ist der Rückgabewert
		///    ungleich null.
		/// </summary>
		/// <param name="documentID">die ID des Dokuments</param>
		/// <param name="topic">Das zugeordnete Thema, falls eines existiert.</param>
		/// <param name="document">Das Doukument, das der ID zugeordnet ist.</param>
		/// <returns></returns>
		private ActionResult CheckConstraints(int documentID, [CanBeNull] out Topic topic, out Document document)
		{
			topic = null;
			document = db.Documents.Find(documentID);

			if (document == null)
				return HTTPStatus(HttpStatusCode.NotFound, "Dokument-ID nicht gefunden.");

			if (document.LockTime != null)
				return HTTPStatus(HttpStatusCode.Forbidden, "Das Dokument ist derzeit gesperrt.");

			if (!IsAuthorizedFor(document.ParentContainer))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Container nicht berechtigt!");

			topic = document.ParentContainer.Topic;
			if (topic == null)
				return null; // Alle Checks bestanden

			if (IsTopicLocked(topic.ID))
				return HTTPStatus(HttpStatusCode.Forbidden, "Da das Thema gesperrt ist, können Sie keine Dateien hochladen.");

			if (topic.IsReadOnly)
			{
				return HTTPStatus(HttpStatusCode.Forbidden,
					"Da das Thema schreibgeschützt ist, können Sie keine Dateien bearbeiten.");
			}
			return null;
		}

		/// <summary>
		///    Beginnt die nahtlose Bearbeitung eines Dokuments. Hierzu wird die aktuelle Revision kopiert und dem Anwender werden
		///    Schreibrechte eingeräumt. Nach Abschluss seiner Bearbeitung muss der Anwender speichern, um die neue Revision zur
		///    aktuellen zu machen.
		/// </summary>
		/// <param name="id">Die ID des Dokuments, zu dem eine neue Revision erzeugt werden soll.</param>
		//[HttpPost]
		public ActionResult BeginNewRevision(int id) // id = Document.ID
		{
			// Checks
			Document document;
			Topic topic;
			var actionResult = CheckConstraints(id, out topic, out document);
			if (actionResult != null)
				return actionResult;

			if (!EnableSeamlessEditing)
				return HTTPStatus(HttpStatusCode.BadRequest, "Dieser Vorgang ist global deaktiviert.");

			if (!isInternetExplorer || !OfficeExtensions.Contains(document.LatestRevision.Extension))
				return HTTPStatus(HttpStatusCode.BadRequest, "Dieser Vorgang ist nur mit MSIE und Office-Dokumenten zulässig.");
			//---------------------------------------------------------------

			var now = DateTime.Now;

			document.LockUserID = GetCurrentUserID();
			document.LockTime = now;

			var revision = new Revision
			{
				ParentDocument = document,
				SafeName = document.LatestRevision.SafeName,
				FileSize = 0,
				UploaderID = GetCurrentUserID(),
				Extension = document.LatestRevision.Extension,
				GUID = Guid.NewGuid(),
				Created = now.AddMilliseconds(100)
			};
			document.Revisions.Add(revision);

			try
			{
				db.SaveChanges(); // Damit die Revision seine ID bekommt. Diese wird anschließend im Dateinamen hinterlegt
				var sourcePath = Path.Combine(Serverpath, document.LatestRevision.FileName);
				var destPath = Path.Combine(TemporaryServerpath, revision.FileName);
				System.IO.File.Copy(sourcePath, destPath);
			}
			catch (DbEntityValidationException ex)
			{
				return HTTPStatus(HttpStatusCode.InternalServerError, ErrorMessageFromException(ex));
			}
			catch (IOException ex)
			{
				return HTTPStatus(HttpStatusCode.InternalServerError, "Dateisystemfehler: " + ex.Message);
			}
			return HTTPStatus(HttpStatusCode.Created, "file://" + _hostname + "/Temp/" + revision.FileName);
		}

		public ActionResult CancelNewRevision(int id)  // id = Document.ID
		{
			if (!EnableSeamlessEditing)
				return HTTPStatus(HttpStatusCode.BadRequest, "Dieser Vorgang ist global deaktiviert.");

			var container = db.Documents.Find(id)?.ParentContainer;
			if (container == null)
				return HTTPStatus(HttpStatusCode.NotFound, "Container nicht gefunden");
			if (!IsAuthorizedFor(container))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diese Dokumente nicht berechtigt!");
			
			try
			{
				ForceReleaseLock(id);
			}
			catch (IOException)
			{
				// The file is still in use
				return HTTPStatus(HttpStatusCode.Conflict, "Dieser Vorgang kann nicht ausgeführt werden, da die Datei noch in Verwendung ist. Bitte schließen Sie die Datei und versuchen Sie es erneut.");
			}
			return RedirectToAction("Details", new { id });
		}

		public static void ForceReleaseLock(int documentID)
		{
			using (var db = new DataContext())
			{
				var doc = db.Documents.Find(documentID);
				if (doc == null)
					return;

				var cutoff = doc.LatestRevision.Created;
				var unused = doc.Revisions.Where(r => r.Created > cutoff).ToArray();

				if (unused.Length <= 0)
					return;

				foreach (var revision in unused)
					System.IO.File.Delete(Path.Combine(TemporaryServerpath, revision.FileName));

				var unusedids = unused.Select(r => r.ID).ToArray();
				db.Revisions.Where(r => unusedids.Contains(r.ID)).Delete();

				doc.LockTime = null;
				doc.LockUserID = null;
				db.SaveChanges();
			}
		}

		public ActionResult FinishNewRevision(int id) // id = Document.ID
		{
			if (!EnableSeamlessEditing)
				return HTTPStatus(HttpStatusCode.BadRequest, "Dieser Vorgang ist global deaktiviert.");

			// Checks
			var document = db.Documents.Find(id);

			if (document == null)
				return HTTPStatus(HttpStatusCode.NotFound, "Dokument-ID nicht gefunden.");

			if (document.LockTime == null)
				return HTTPStatus(HttpStatusCode.Forbidden, "Das Dokument ist derzeit nicht in Bearbeitung.");

			if (!IsAuthorizedFor(document.ParentContainer))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Container nicht berechtigt!");

			var topic = document.ParentContainer.Topic;
			if (topic != null)
			{
				if (IsTopicLocked(topic.ID))
					return HTTPStatus(HttpStatusCode.Forbidden, "Da das Thema gesperrt ist, können Sie keine Dateien hochladen.");

				if (topic.IsReadOnly)
				{
					return HTTPStatus(HttpStatusCode.Forbidden,
						"Da das Thema schreibgeschützt ist, können Sie keine Dateien bearbeiten.");
				}
			}

			if (GetCurrentUserID() != document.LockUserID)
				return HTTPStatus(HttpStatusCode.Forbidden, "Das Dokument ist auf einen anderen Nutzer gesperrt, Sie sind nicht autorisiert.");
			//---------------------------------------------------------------

			var newrevision = document.Revisions.OrderByDescending(r => r.Created).First();

			var sourcePath = Path.Combine(TemporaryServerpath, newrevision.FileName);
			var destPath = Path.Combine(Serverpath, newrevision.FileName);

			if (newrevision.ID == document.LatestRevisionID || !System.IO.File.Exists(sourcePath))
				return HTTPStatus(HttpStatusCode.InternalServerError, "Vorgang kann nicht abgeschlossen werden, da das Dokument zwar gesperrt, aber die Revison nicht in Bearbeitung ist.");

			newrevision.FileSize = (int)new FileInfo(sourcePath).Length;
			newrevision.Created = DateTime.Now;
			try
			{
				System.IO.File.Move(sourcePath, destPath);
			}
			catch (IOException)
			{
				// The file is still in use
				return HTTPStatus(HttpStatusCode.Conflict, "Dieser Vorgang kann nicht ausgeführt werden, da die Datei noch in Verwendung ist. Bitte schließen Sie die Datei und versuchen Sie es erneut.");
			}

			document.LatestRevisionID = newrevision.ID;
			document.LockTime = null;
			document.LockUserID = null;
			db.SaveChanges();

			return RedirectToAction("Details", new { id });
		}

		[HttpPost]
		public ActionResult _Delete(int documentID)
		{
			var document = db.Documents.Include(d => d.ParentContainer).Include(d => d.LatestRevision).Single(d => d.ID == documentID);

			if (document.Deleted != null)
				return HTTPStatus(422, "Das Objekt befindet sich bereits im Papierkorb.");

			if (!IsAuthorizedFor(document.ParentContainer))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Container nicht berechtigt!");

			var topic = document.ParentContainer.Topic;
			if (topic != null)
			{
				if (IsTopicLocked(topic.ID))
					return HTTPStatus(HttpStatusCode.Forbidden, "Da das Thema gesperrt ist, können Sie keine Dateien bearbeiten.");

				if (topic.IsReadOnly)
					return HTTPStatus(HttpStatusCode.Forbidden,
						"Da das Thema schreibgeschützt ist, können Sie keine Dateien bearbeiten.");
			}

			document.Deleted = DateTime.Now; // In den Papierkorb
			try
			{
				db.SaveChanges();
			}
			catch (DbEntityValidationException e)
			{
				var message = ErrorMessageFromException(e);
				return HTTPStatus(HttpStatusCode.InternalServerError, message);
			}

			return new HttpStatusCodeResult(HttpStatusCode.NoContent);
		}

		/// <summary>
		///    Download oder öffnen einer Datei. Öffnen von Dokumenten funktioniert nur im Internet Explorer.
		/// </summary>
		/// <param name="id">ID der Datei</param>
		/// <returns></returns>
		public ActionResult Download(Guid id) // id = Revision.GUID
		{
			var file = db.Revisions.Include(rev => rev.ParentDocument).FirstOrDefault(rev => rev.GUID == id);
			if (file == null)
				return HttpNotFound("Revision nicht gefunden.");

			if (!IsAuthorizedFor(file.ParentDocument.ParentContainer))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diese Datei nicht berechtigt!");

			if (EnableSeamlessDownload && OfficeExtensions.Contains(file.Extension) && isInternetExplorer)
				return Redirect("file://" + _hostname + "/Uploads/" + file.FileName);

			var cd = new ContentDisposition
			{
				FileName = file.ParentDocument.DisplayName,
				Inline = true
			};
			Response.AppendHeader("Content-Disposition", cd.ToString());

			return File(Path.Combine(Serverpath, file.FileName), MimeMapping.GetMimeMapping(file.FileName));
		}

		/// <summary>
		///    Download der neuesten Version eines Dokuments.
		/// </summary>
		/// <param name="id">ID des Dokuments</param>
		/// <returns></returns>
		[AllowAnonymous]
		public ActionResult DownloadNewest(Guid id) // id = Document.GUID
		{
			var document = db.Documents.FirstOrDefault(doc => doc.GUID == id);
			return document == null ? HttpNotFound("Dokument nicht gefunden.") : Download(document.LatestRevision.GUID);
		}

		public ActionResult _BeginEdit(int documentID)
		{
			var doc = db.Documents.Find(documentID);
			if (doc == null)
				return HttpNotFound();
			if (!IsAuthorizedFor(doc.ParentContainer))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Container nicht berechtigt!");

			var topic = doc.ParentContainer.Topic;
			if (topic != null && topic.IsReadOnly)
				return HTTPStatus(HttpStatusCode.Forbidden, "Das Thema ist schreibgeschützt!");

			return PartialView("_NameEditor", doc);
		}

		public PartialViewResult _FetchDisplayName(int documentID)
		{
			var document = db.Documents.Find(documentID);

			var url = new UrlHelper(ControllerContext.RequestContext).Action("DownloadNewest", "Attachments",
				new { id = document.GUID });
			return PartialView("_NameDisplay", Tuple.Create(new MvcHtmlString(url), document.DisplayName));
		}

		public string FetchNewestRevURL(int documentID)
		{
			var file = db.Documents.Find(documentID).LatestRevision;
			return "file://" + _hostname + "/Uploads/" + file.FileName;
		}

		public ActionResult _FetchTableRow(int documentID)
		{
			var document = db.Documents.Find(documentID);

			if (document == null)
				return HttpNotFound();
			if (!IsAuthorizedFor(document.ParentContainer))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Container nicht berechtigt!");

			ViewBag.KnownExtensions = KnownExtensions;
			ViewBag.OfficeExtensions = OfficeExtensions;
			ViewBag.SeamlessEnabled = EnableSeamlessEditing && isInternetExplorer;

			return PartialView("_AttachmentRow", document);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult _SubmitEdit(int id, string displayName) // id = Document.ID
		{
			var document = db.Documents.Find(id);
			if (document == null)
				return HttpNotFound();
			if (!IsAuthorizedFor(document.ParentContainer))
				return HTTPStatus(HttpStatusCode.Forbidden, "Sie sind für diesen Container nicht berechtigt!");

			var topic = document.ParentContainer.Topic;

			if (topic != null && topic.IsReadOnly)
				return HTTPStatus(HttpStatusCode.Forbidden, "Das Thema ist schreibgeschützt!");

			displayName = Path.ChangeExtension(displayName, Path.GetExtension(document.LatestRevision.FileName));
			document.DisplayName = displayName;
			db.SaveChanges();

			var url = new UrlHelper(ControllerContext.RequestContext).Action("DownloadNewest", "Attachments",
				new { id = document.GUID });
			return PartialView("_NameDisplay", Tuple.Create(new MvcHtmlString(url), document.DisplayName));
		}
	}
}