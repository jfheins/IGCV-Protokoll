namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class deleted_unused_fk_document : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Revision", "Document_ID", "dbo.Document");
            DropIndex("dbo.Revision", new[] { "Document_ID" });
            DropColumn("dbo.Revision", "Document_ID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Revision", "Document_ID", c => c.Int());
            CreateIndex("dbo.Revision", "Document_ID");
            AddForeignKey("dbo.Revision", "Document_ID", "dbo.Document", "ID");
        }
    }
}
