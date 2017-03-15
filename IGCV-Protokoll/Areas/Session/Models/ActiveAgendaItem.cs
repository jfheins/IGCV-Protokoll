using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using IGCV_Protokoll.Areas.Session.Models;

namespace IGCV_Protokoll.Areas.Session.Models
{
    public class ActiveAgendaItem
    {
	    private sealed class PositionRelationalComparer : Comparer<ActiveAgendaItem>
	    {
		    public override int Compare(ActiveAgendaItem x, ActiveAgendaItem y)
		    {
			    if (ReferenceEquals(x, y)) return 0;
			    if (ReferenceEquals(null, y)) return 1;
			    if (ReferenceEquals(null, x)) return -1;
			    return x.Position.CompareTo(y.Position);
		    }
	    }

	    public static Comparer<ActiveAgendaItem> PositionComparer { get; } = new PositionRelationalComparer();

	    public int ID { get; set; }

        public ActiveSession Parent { get; set; }

        [ForeignKey("Parent")]
        public int ParentID { get; set; }

        [DisplayName("Titel")]
        [Required]
        public string Title { get; set; }

        [DisplayName("Beschreibung")]
        [DataType(DataType.MultilineText)]
        public virtual string Description { get; set; }
        [DisplayName("Kommentar")]
        [DataType(DataType.MultilineText)]
        public virtual string Comment { get; set; }

        public int Position { get; set; }

        public static ActiveAgendaItem FromTemplate(AgendaItem templateItem, ActiveSession parent)
        {
            return new ActiveAgendaItem
            {
                Title = templateItem.Title,
                Description = templateItem.Description,
                Comment =  templateItem.Placeholder,
                Position = templateItem.Position,
                Parent = parent
            };
        }
    }
}
