using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using IGCV_Protokoll.Areas.Administration.Models;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.Areas.Session.Models
{
    public class AgendaTemplate
    {
        public AgendaTemplate()
        {
            // ReSharper disable VirtualMemberCallInConstructor
            SessionTypes = new List<SessionType>();
            AgendaItems = new List<AgendaItem>();
            // ReSharper restore VirtualMemberCallInConstructor
        }

        public int ID { get; set; }

        [DisplayName("Templatename")]
        public virtual string Name { get; set; }

        [InverseProperty("Agenda")]
        public virtual ICollection<SessionType> SessionTypes { get; set; }

        public virtual ICollection<AgendaItem> AgendaItems { get; set; }
    }
}