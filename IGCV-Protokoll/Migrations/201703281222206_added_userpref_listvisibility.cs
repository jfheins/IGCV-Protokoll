namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_userpref_listvisibility : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "Settings_ShowEvents", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.User", "Settings_ShowResearchProposals", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.User", "Settings_ShowIndustryProjects", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.User", "Settings_ShowOpenings", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.User", "Settings_ShowHolidays", c => c.Boolean(nullable: false, defaultValue: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.User", "Settings_ShowHolidays");
            DropColumn("dbo.User", "Settings_ShowOpenings");
            DropColumn("dbo.User", "Settings_ShowIndustryProjects");
            DropColumn("dbo.User", "Settings_ShowResearchProposals");
            DropColumn("dbo.User", "Settings_ShowEvents");
        }
    }
}
