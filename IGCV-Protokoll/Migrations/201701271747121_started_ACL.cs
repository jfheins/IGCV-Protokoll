namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class started_ACL : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ACLItem",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        ParentId = c.Int(nullable: false),
                        AdEntityID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.AdEntity", t => t.AdEntityID, cascadeDelete: true)
                .ForeignKey("dbo.ACL", t => t.ParentId, cascadeDelete: true)
                .Index(t => new { t.ParentId, t.AdEntityID }, unique: true, name: "IX_ACLEntityunique");
            
            CreateTable(
                "dbo.AdEntity",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Guid = c.Guid(nullable: false),
                        Name = c.String(nullable: false),
                        SamAccountName = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .Index(t => t.Guid, unique: true, name: "guid_index");
            
            CreateTable(
                "dbo.AdEntityUser",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        AdEntityID = c.Int(nullable: false),
                        UserID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.User", t => t.UserID, cascadeDelete: true)
                .ForeignKey("dbo.AdEntity", t => t.AdEntityID, cascadeDelete: true)
                .Index(t => t.AdEntityID)
                .Index(t => t.UserID);
            
            CreateTable(
                "dbo.ACL",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                    })
                .PrimaryKey(t => t.ID);
            
            AddColumn("dbo.Topic", "AclID", c => c.Int());
            CreateIndex("dbo.Topic", "AclID");
            AddForeignKey("dbo.Topic", "AclID", "dbo.ACL", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AdEntityUser", "AdEntityID", "dbo.AdEntity");
            DropForeignKey("dbo.Topic", "AclID", "dbo.ACL");
            DropForeignKey("dbo.ACLItem", "ParentId", "dbo.ACL");
            DropForeignKey("dbo.AdEntityUser", "UserID", "dbo.User");
            DropForeignKey("dbo.ACLItem", "AdEntityID", "dbo.AdEntity");
            DropIndex("dbo.Topic", new[] { "AclID" });
            DropIndex("dbo.AdEntityUser", new[] { "UserID" });
            DropIndex("dbo.AdEntityUser", new[] { "AdEntityID" });
            DropIndex("dbo.AdEntity", "guid_index");
            DropIndex("dbo.ACLItem", "IX_ACLEntityunique");
            DropColumn("dbo.Topic", "AclID");
            DropTable("dbo.ACL");
            DropTable("dbo.AdEntityUser");
            DropTable("dbo.AdEntity");
            DropTable("dbo.ACLItem");
        }
    }
}
