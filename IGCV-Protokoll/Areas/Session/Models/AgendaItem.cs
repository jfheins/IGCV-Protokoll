using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IGCV_Protokoll.Areas.Session.Models
{
    public class AgendaItem
    {
        public int ID { get; set; }

        public AgendaTemplate Parent { get; set; }

        [Index("IX_ParentIDPosition", 1, IsUnique = true)]
        [ForeignKey("Parent")]
        public int ParentID { get; set; }

        [DisplayName("Titel")]
        [Required]
        public string Title { get; set; }

        [DisplayName("Anmerkung")]
        [DataType(DataType.MultilineText)]
		public virtual string Description { get; set; }

        [DisplayName("Beschreibung")]
        [DataType(DataType.MultilineText)]
        public virtual string Placeholder { get; set; }

        [Index("IX_ParentIDPosition", 2, IsUnique = true)]
        public int Position { get; set; }
    }
}