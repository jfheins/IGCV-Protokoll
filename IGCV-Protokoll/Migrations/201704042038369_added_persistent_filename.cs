namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_persistent_filename : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Revision", "DiskName", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Revision", "DiskName");
        }
    }
}
