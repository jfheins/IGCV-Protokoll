namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class agenda_added : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ActiveAgendaItem",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Description = c.String(),
                        Comment = c.String(),
                        Position = c.Int(nullable: false),
                        Parent_ID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.ActiveSession", t => t.Parent_ID)
                .Index(t => t.Parent_ID);
            
            CreateTable(
                "dbo.AgendaTemplate",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.AgendaItem",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Description = c.String(),
                        Placeholder = c.String(),
                        Position = c.Int(nullable: false),
                        Parent_ID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.AgendaTemplate", t => t.Parent_ID)
                .Index(t => t.Parent_ID);
            
            AddColumn("dbo.SessionType", "Agenda_ID", c => c.Int());
            CreateIndex("dbo.SessionType", "Agenda_ID");
            AddForeignKey("dbo.SessionType", "Agenda_ID", "dbo.AgendaTemplate", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.SessionType", "Agenda_ID", "dbo.AgendaTemplate");
            DropForeignKey("dbo.AgendaItem", "Parent_ID", "dbo.AgendaTemplate");
            DropForeignKey("dbo.ActiveAgendaItem", "Parent_ID", "dbo.ActiveSession");
            DropIndex("dbo.AgendaItem", new[] { "Parent_ID" });
            DropIndex("dbo.SessionType", new[] { "Agenda_ID" });
            DropIndex("dbo.ActiveAgendaItem", new[] { "Parent_ID" });
            DropColumn("dbo.SessionType", "Agenda_ID");
            DropTable("dbo.AgendaItem");
            DropTable("dbo.AgendaTemplate");
            DropTable("dbo.ActiveAgendaItem");
        }
    }
}
