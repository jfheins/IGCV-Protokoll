namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class made_agenda_Description_optional : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.AgendaItem", "Description", c => c.String());
            AlterColumn("dbo.ActiveAgendaItem", "Description", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.ActiveAgendaItem", "Description", c => c.String(nullable: false));
            AlterColumn("dbo.AgendaItem", "Description", c => c.String(nullable: false));
        }
    }
}
