namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class adentity_tree_structure : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AdEntity", "ParentID", c => c.Int());
            CreateIndex("dbo.AdEntity", "ParentID");
            AddForeignKey("dbo.AdEntity", "ParentID", "dbo.AdEntity", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AdEntity", "ParentID", "dbo.AdEntity");
            DropIndex("dbo.AdEntity", new[] { "ParentID" });
            DropColumn("dbo.AdEntity", "ParentID");
        }
    }
}
