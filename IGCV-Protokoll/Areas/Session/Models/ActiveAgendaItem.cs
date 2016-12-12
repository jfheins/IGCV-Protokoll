using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using IGCV_Protokoll.Areas.Session.Models;

namespace IGCV_Protokoll.Areas.Session.Models
{
    public class ActiveAgendaItem
    {
        public int ID { get; set; }

        public ActiveSession Parent { get; set; }

        [DisplayName("Beschreibung")]
        public virtual string Description { get; set; }
        [DisplayName("Kommentar")]
        public virtual string Comment { get; set; }

        public int Position { get; set; }

        public static ActiveAgendaItem FromTemplate(AgendaItem templateItem, ActiveSession parent)
        {
            return new ActiveAgendaItem
            {
                Description = templateItem.Description,
                Comment =  templateItem.Placeholder,
                Position = templateItem.Position,
                Parent = parent
            };
        }
    }
}