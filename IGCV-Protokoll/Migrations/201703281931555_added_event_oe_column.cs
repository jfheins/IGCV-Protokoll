namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_event_oe_column : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.L_Event", "OrganizationUnit", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.L_Event", "OrganizationUnit");
        }
    }
}
