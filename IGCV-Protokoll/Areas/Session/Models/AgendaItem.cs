using System.ComponentModel;

namespace IGCV_Protokoll.Areas.Session.Models
{
    public class AgendaItem
    {
        public int ID { get; set; }

        public AgendaTemplate Parent { get; set; }

        [DisplayName("Beschreibung")]
        public virtual string Description { get; set; }
        [DisplayName("Standardtext")]
        public virtual string Placeholder { get; set; }

        public int Position { get; set; }
    }
}