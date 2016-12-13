namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class agenda_attribute : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ActiveAgendaItem", "Title", c => c.String(nullable: false));
            AddColumn("dbo.AgendaItem", "Title", c => c.String(nullable: false));
            AlterColumn("dbo.ActiveAgendaItem", "Description", c => c.String(nullable: false));
            AlterColumn("dbo.AgendaTemplate", "Name", c => c.String(nullable: false));
            AlterColumn("dbo.AgendaItem", "Description", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.AgendaItem", "Description", c => c.String());
            AlterColumn("dbo.AgendaTemplate", "Name", c => c.String());
            AlterColumn("dbo.ActiveAgendaItem", "Description", c => c.String());
            DropColumn("dbo.AgendaItem", "Title");
            DropColumn("dbo.ActiveAgendaItem", "Title");
        }
    }
}
