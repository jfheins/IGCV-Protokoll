namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_principal_type : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AdEntity", "Type", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AdEntity", "Type");
        }
    }
}
