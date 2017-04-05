namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_topic_type : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Topic", "TopicType", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Topic", "TopicType");
        }
    }
}
