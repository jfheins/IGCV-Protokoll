using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IGCV_Protokoll.Models;
using IGCV_Protokoll.util;

namespace IGCV_Protokoll.Areas.Session.Models.Lists
{
    /// <summary>
    ///    Forschungsantrag
    /// </summary>
    [Table("L_ResearchProposal")]
    public class ResearchProposal : BaseItem, IFileContainer
	{
        public ResearchProposal()
        {
            DueDate = DateTime.Today;
        }


        [Required]
        [DisplayName("Förderer")]
        public string Sponsor { get; set; }

        [Required]
        [DisplayName("Akronym")]
        public string Akronym { get; set; }

        [DisplayName("Einreichung am")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DueDate { get; set; }

        [DisplayName("ILK")]
        public virtual User Ilk { get; set; }

        [ForeignKey("Ilk")]
        public int IlkID { get; set; }

        [Required]
        [DisplayName("Mitarbeiter")]
        public string Employee { get; set; }

        [Required]
        [DisplayName("Status")]
        public ResearchProposalState Status { get; set; }

        [Required(AllowEmptyStrings = true)]
        [DisplayName("Anmerkungen")]
		[DataType(DataType.MultilineText)]
		public string Description { get; set; }

		[DisplayName("Dokumente")]
		public virtual DocumentContainer Documents { get; set; }

		[ForeignKey("Documents")]
		public int DocumentsID { get; set; }

		public string getTitle() => $"Forschungsantrag: {Employee}";
	}
    public enum ResearchProposalState
    {
        [Display(Name = "Skizze")]
        Sketch,
        [Display(Name = "Vollantrag")]
        FullProposal,
        [Display(Name = "Verteidigung")]
        Presentation
    }
}