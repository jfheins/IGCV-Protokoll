using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web.Hosting;
using IGCV_Protokoll.Models;
using Mvc.Mailer;

namespace IGCV_Protokoll.Mailers
{
	public class UserMailer : MailerBase
	{
		private readonly string FQDN = "http://" + Dns.GetHostName() + ".igcv.fraunhofer.de";

		public UserMailer()
		{
			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			MasterName = "~/Views/UserMailer/_Layout.cshtml";
		}

		public virtual void SendWelcome(User u)
		{
			if (u.EmailAddress != null)
			{
				ViewData.Model = u.EmailName;
				ViewBag.Host = FQDN;
				var mail = Populate(x =>
				{
					x.Subject = "Wilkommen beim IGCV-Protokoll";
					x.ViewName = "Welcome";
					x.To.Add(u.EmailAddress);
				});
				HostingEnvironment.QueueBackgroundWorkItem(ct => mail.SendAsync());
			}
		}

		public virtual Task SendNewAssignment(Assignment assignment)
		{
			ViewData.Model = assignment;
			ViewBag.Host = FQDN;
			var mail = Populate(x =>
			{
				x.Subject = string.Format("Neue Aufgabe »{0}« im IGCV-Protokoll", assignment.Title);
				x.ViewName = "NewAssignment";
				x.To.Add(assignment.Owner.EmailAddress);
			});
			return mail.SendAsync();
		}

		public void SendAssignmentReminder(Assignment assignment)
		{
			ViewData.Model = assignment;
			ViewBag.Host = FQDN;
			var mail = Populate(x =>
			{
				x.Subject = string.Format("Die Aufgabe »{0}« wird bald fällig", assignment.Title);
				x.ViewName = "AssignmentReminder";
				x.To.Add(assignment.Owner.EmailAddress);
			});
			mail.Send();
		}

		public void SendAssignmentOverdue(Assignment assignment)
		{
			ViewData.Model = assignment;
			ViewBag.Host = FQDN;
			var mail = Populate(x =>
			{
				x.Subject = string.Format("Die Aufgabe »{0}« ist überfällig!", assignment.Title);
				x.ViewName = "AssignmentOverdue";
				x.To.Add(assignment.Owner.EmailAddress);
			});
			mail.Send();
		}

		public void SendSessionReport(IEnumerable<Topic> topics, SessionReport report, byte[] pdfReport)
		{
			ViewData.Model = topics;
			ViewBag.Report = report;
			ViewBag.Host = FQDN;

			var mails = new List<MvcMailMessage>();

			var pdfStream = new MemoryStream(pdfReport, false);
			var pdfAttachment = new Attachment(pdfStream, report.FileName, "application/pdf");

			foreach (var recipient in report.SessionType.Attendees.Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.EmailAddress)))
			{
				var isAbsent = !report.PresentUsers.Contains(recipient);
				if (recipient.Settings.ReportOccasions == SessionReportOccasions.Always
					|| (recipient.Settings.ReportOccasions == SessionReportOccasions.WhenAbsent && isAbsent))
				{
					var mail = new MvcMailMessage
					{
						Subject = $"Eine Sitzung des Typs »{report.SessionType.Name}« wurde durchgeführt",
						ViewName = "NewSessionReport"
					};
					mail.To.Add(recipient.EmailAddress);
					if (recipient.Settings.ReportAttachPDF)
						mail.Attachments.Add(pdfAttachment);

					PopulateBody(mail, mail.ViewName, mail.MasterName, mail.LinkedResources);
					mails.Add(mail);
				}
			}
			foreach (var mail in mails)
				mail.Send();
		}
	}
}