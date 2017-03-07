namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_dc_title : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DocumentContainer", "Title", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.DocumentContainer", "Title");
        }
    }
}
