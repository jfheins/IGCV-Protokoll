using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;
using IGCV_Protokoll.Areas.Administration.Models;
using IGCV_Protokoll.Areas.Session.Models;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.Areas.Administration.ViewModels
{
    public class SessionTypeVM
    {
        public SessionTypeVM()
        {
            Active = true;
        }

        public int ID { get; set; }

        [DisplayName("Name")]
        public string Name { get; set; }

        [DisplayName("Auswählbar")]
        public bool Active { get; set; }

        [DisplayName("Stammteilnehmer")]
        public Dictionary<User, bool> UserDict { get; set; }

        [DisplayName("Agenda")]
        public virtual int? AgendaID { get; set; }
        public SelectList AgendaList { get; set; }

        public SessionType updateModel(SessionType m)
        {
            m.ID = ID;
            m.Name = Name;
            m.Active = Active;
            m.AgendaID = AgendaID;
            return m;
        }

        public static SessionTypeVM fromModel(SessionType model)
        {
            return new SessionTypeVM
            {
                ID = model.ID,
                Name = model.Name,
                Active = model.Active,
                AgendaID = model.AgendaID
            };
        }
    }
}