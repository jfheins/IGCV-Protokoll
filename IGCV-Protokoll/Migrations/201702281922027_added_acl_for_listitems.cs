namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_acl_for_listitems : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.L_Conference", "AclID", c => c.Int());
            AddColumn("dbo.L_EmployeePresentation", "AclID", c => c.Int());
            AddColumn("dbo.L_Event", "AclID", c => c.Int());
            AddColumn("dbo.L_Extension", "AclID", c => c.Int());
            AddColumn("dbo.L_Holiday", "AclID", c => c.Int());
            AddColumn("dbo.L_IlkDay", "AclID", c => c.Int());
            AddColumn("dbo.L_IlkMeeting", "AclID", c => c.Int());
            AddColumn("dbo.L_IndustryProject", "AclID", c => c.Int());
            AddColumn("dbo.L_Opening", "AclID", c => c.Int());
            AddColumn("dbo.L_ResearchProposal", "AclID", c => c.Int());
            CreateIndex("dbo.L_Conference", "AclID");
            CreateIndex("dbo.L_EmployeePresentation", "AclID");
            CreateIndex("dbo.L_Event", "AclID");
            CreateIndex("dbo.L_Extension", "AclID");
            CreateIndex("dbo.L_Holiday", "AclID");
            CreateIndex("dbo.L_IlkDay", "AclID");
            CreateIndex("dbo.L_IlkMeeting", "AclID");
            CreateIndex("dbo.L_IndustryProject", "AclID");
            CreateIndex("dbo.L_Opening", "AclID");
            CreateIndex("dbo.L_ResearchProposal", "AclID");
            AddForeignKey("dbo.L_Conference", "AclID", "dbo.ACL", "ID");
            AddForeignKey("dbo.L_EmployeePresentation", "AclID", "dbo.ACL", "ID");
            AddForeignKey("dbo.L_Event", "AclID", "dbo.ACL", "ID");
            AddForeignKey("dbo.L_Extension", "AclID", "dbo.ACL", "ID");
            AddForeignKey("dbo.L_Holiday", "AclID", "dbo.ACL", "ID");
            AddForeignKey("dbo.L_IlkDay", "AclID", "dbo.ACL", "ID");
            AddForeignKey("dbo.L_IlkMeeting", "AclID", "dbo.ACL", "ID");
            AddForeignKey("dbo.L_IndustryProject", "AclID", "dbo.ACL", "ID");
            AddForeignKey("dbo.L_Opening", "AclID", "dbo.ACL", "ID");
            AddForeignKey("dbo.L_ResearchProposal", "AclID", "dbo.ACL", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.L_ResearchProposal", "AclID", "dbo.ACL");
            DropForeignKey("dbo.L_Opening", "AclID", "dbo.ACL");
            DropForeignKey("dbo.L_IndustryProject", "AclID", "dbo.ACL");
            DropForeignKey("dbo.L_IlkMeeting", "AclID", "dbo.ACL");
            DropForeignKey("dbo.L_IlkDay", "AclID", "dbo.ACL");
            DropForeignKey("dbo.L_Holiday", "AclID", "dbo.ACL");
            DropForeignKey("dbo.L_Extension", "AclID", "dbo.ACL");
            DropForeignKey("dbo.L_Event", "AclID", "dbo.ACL");
            DropForeignKey("dbo.L_EmployeePresentation", "AclID", "dbo.ACL");
            DropForeignKey("dbo.L_Conference", "AclID", "dbo.ACL");
            DropIndex("dbo.L_ResearchProposal", new[] { "AclID" });
            DropIndex("dbo.L_Opening", new[] { "AclID" });
            DropIndex("dbo.L_IndustryProject", new[] { "AclID" });
            DropIndex("dbo.L_IlkMeeting", new[] { "AclID" });
            DropIndex("dbo.L_IlkDay", new[] { "AclID" });
            DropIndex("dbo.L_Holiday", new[] { "AclID" });
            DropIndex("dbo.L_Extension", new[] { "AclID" });
            DropIndex("dbo.L_Event", new[] { "AclID" });
            DropIndex("dbo.L_EmployeePresentation", new[] { "AclID" });
            DropIndex("dbo.L_Conference", new[] { "AclID" });
            DropColumn("dbo.L_ResearchProposal", "AclID");
            DropColumn("dbo.L_Opening", "AclID");
            DropColumn("dbo.L_IndustryProject", "AclID");
            DropColumn("dbo.L_IlkMeeting", "AclID");
            DropColumn("dbo.L_IlkDay", "AclID");
            DropColumn("dbo.L_Holiday", "AclID");
            DropColumn("dbo.L_Extension", "AclID");
            DropColumn("dbo.L_Event", "AclID");
            DropColumn("dbo.L_EmployeePresentation", "AclID");
            DropColumn("dbo.L_Conference", "AclID");
        }
    }
}
