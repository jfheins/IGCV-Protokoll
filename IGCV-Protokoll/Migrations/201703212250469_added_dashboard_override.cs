namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_dashboard_override : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TopicVisibilityOverride",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        TopicID = c.Int(nullable: false),
                        UserID = c.Int(nullable: false),
                        Visibility = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Topic", t => t.TopicID, cascadeDelete: true)
                .ForeignKey("dbo.User", t => t.UserID, cascadeDelete: false)
                .Index(t => t.TopicID)
                .Index(t => t.UserID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TopicVisibilityOverride", "UserID", "dbo.User");
            DropForeignKey("dbo.TopicVisibilityOverride", "TopicID", "dbo.Topic");
            DropIndex("dbo.TopicVisibilityOverride", new[] { "UserID" });
            DropIndex("dbo.TopicVisibilityOverride", new[] { "TopicID" });
            DropTable("dbo.TopicVisibilityOverride");
        }
    }
}
