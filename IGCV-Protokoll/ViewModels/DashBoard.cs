using System.Collections.Generic;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.ViewModels
{
	public class DashBoard
	{
		public IEnumerable<Topic> MyTopics { get; set; }

		public IEnumerable<Assignment> MyToDos { get; set; }

		public IEnumerable<Assignment> MyDuties { get; set; }
		public IEnumerable<PushNotification> Notifications { get; set; }
	}
}