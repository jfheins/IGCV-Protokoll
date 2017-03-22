namespace IGCV_Protokoll.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class added_topic_links : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TopicLink",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        LeftTopicID = c.Int(nullable: false),
                        RightTopicID = c.Int(nullable: false),
                        LinkTemplateID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Topic", t => t.LeftTopicID, cascadeDelete: true)
                .ForeignKey("dbo.TopicLinkTemplate", t => t.LinkTemplateID, cascadeDelete: true)
                .ForeignKey("dbo.Topic", t => t.RightTopicID, cascadeDelete: false)
                .Index(t => t.LeftTopicID)
                .Index(t => t.RightTopicID)
                .Index(t => t.LinkTemplateID);
            
            CreateTable(
                "dbo.TopicLinkTemplate",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        LeftText = c.String(nullable: false),
                        RightText = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.ID);

			Sql(@"INSERT INTO [dbo].[TopicLinkTemplate]	([LeftText], [RightText])
							VALUES  ('Verursacht', 'Wird verursacht von'),
									('Enthält', 'Ist Teil von'),
									('Dupliziert', 'Wird dupliziert von'),
									('Ist verwandt mit', 'Ist verwandt mit'),
									('Ist ähnlich zu', 'Ist ähnlich zu')");
		}
        
        public override void Down()
        {
            DropForeignKey("dbo.TopicLink", "RightTopicID", "dbo.Topic");
            DropForeignKey("dbo.TopicLink", "LinkTemplateID", "dbo.TopicLinkTemplate");
            DropForeignKey("dbo.TopicLink", "LeftTopicID", "dbo.Topic");
            DropIndex("dbo.TopicLink", new[] { "LinkTemplateID" });
            DropIndex("dbo.TopicLink", new[] { "RightTopicID" });
            DropIndex("dbo.TopicLink", new[] { "LeftTopicID" });
            DropTable("dbo.TopicLinkTemplate");
            DropTable("dbo.TopicLink");
        }
    }
}
