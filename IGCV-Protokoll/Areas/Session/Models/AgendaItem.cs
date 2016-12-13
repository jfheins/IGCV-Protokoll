using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IGCV_Protokoll.Areas.Session.Models
{
    public class AgendaItem
    {
        public int ID { get; set; }

        public AgendaTemplate Parent { get; set; }

        [DisplayName("Titel")]
        [Required]
        public string Title { get; set; }

        [DisplayName("Beschreibung")]
        [DataType(DataType.MultilineText)]
        [Required]
        public virtual string Description { get; set; }
        [DisplayName("Standardtext")]
        [DataType(DataType.MultilineText)]
        public virtual string Placeholder { get; set; }

        public int Position { get; set; }
    }
}