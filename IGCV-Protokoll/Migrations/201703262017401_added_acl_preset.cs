namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_acl_preset : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "Settings_AclTreePreset", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.User", "Settings_AclTreePreset");
        }
    }
}
