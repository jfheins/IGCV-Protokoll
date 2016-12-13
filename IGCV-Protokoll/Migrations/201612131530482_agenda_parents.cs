namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class agenda_parents : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ActiveAgendaItem", "Parent_ID", "dbo.ActiveSession");
            DropForeignKey("dbo.AgendaItem", "Parent_ID", "dbo.AgendaTemplate");
            DropIndex("dbo.ActiveAgendaItem", new[] { "Parent_ID" });
            DropIndex("dbo.AgendaItem", new[] { "Parent_ID" });
            RenameColumn(table: "dbo.ActiveAgendaItem", name: "Parent_ID", newName: "ParentID");
            RenameColumn(table: "dbo.AgendaItem", name: "Parent_ID", newName: "ParentID");
            AlterColumn("dbo.ActiveAgendaItem", "ParentID", c => c.Int(nullable: false));
            AlterColumn("dbo.AgendaItem", "ParentID", c => c.Int(nullable: false));
            CreateIndex("dbo.ActiveAgendaItem", "ParentID");
            CreateIndex("dbo.AgendaItem", new[] { "ParentID", "Position" }, unique: true, name: "IX_ParentIDPosition");
            AddForeignKey("dbo.ActiveAgendaItem", "ParentID", "dbo.ActiveSession", "ID", cascadeDelete: true);
            AddForeignKey("dbo.AgendaItem", "ParentID", "dbo.AgendaTemplate", "ID", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AgendaItem", "ParentID", "dbo.AgendaTemplate");
            DropForeignKey("dbo.ActiveAgendaItem", "ParentID", "dbo.ActiveSession");
            DropIndex("dbo.AgendaItem", "IX_ParentIDPosition");
            DropIndex("dbo.ActiveAgendaItem", new[] { "ParentID" });
            AlterColumn("dbo.AgendaItem", "ParentID", c => c.Int());
            AlterColumn("dbo.ActiveAgendaItem", "ParentID", c => c.Int());
            RenameColumn(table: "dbo.AgendaItem", name: "ParentID", newName: "Parent_ID");
            RenameColumn(table: "dbo.ActiveAgendaItem", name: "ParentID", newName: "Parent_ID");
            CreateIndex("dbo.AgendaItem", "Parent_ID");
            CreateIndex("dbo.ActiveAgendaItem", "Parent_ID");
            AddForeignKey("dbo.AgendaItem", "Parent_ID", "dbo.AgendaTemplate", "ID");
            AddForeignKey("dbo.ActiveAgendaItem", "Parent_ID", "dbo.ActiveSession", "ID");
        }
    }
}
