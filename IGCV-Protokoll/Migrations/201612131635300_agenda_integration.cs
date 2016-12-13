namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class agenda_integration : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.SessionType", name: "Agenda_ID", newName: "AgendaID");
            RenameIndex(table: "dbo.SessionType", name: "IX_Agenda_ID", newName: "IX_AgendaID");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.SessionType", name: "IX_AgendaID", newName: "IX_Agenda_ID");
            RenameColumn(table: "dbo.SessionType", name: "AgendaID", newName: "Agenda_ID");
        }
    }
}
