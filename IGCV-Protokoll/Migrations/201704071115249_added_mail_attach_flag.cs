namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_mail_attach_flag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "Settings_ReportAttachPDF", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.User", "Settings_ReportAttachPDF");
        }
    }
}
