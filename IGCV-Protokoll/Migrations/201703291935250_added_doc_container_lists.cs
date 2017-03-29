namespace IGCV_Protokoll.Migrations
{
	using System;
	using System.Data.Entity.Migrations;

	public partial class added_doc_container_lists : DbMigration
	{
		public override void Up()
		{
			AddColumn("dbo.L_Event", "DocumentsID", c => c.Int(nullable: false));
			AddColumn("dbo.L_Holiday", "DocumentsID", c => c.Int(nullable: false));
			AddColumn("dbo.L_IndustryProject", "DocumentsID", c => c.Int(nullable: false));
			AddColumn("dbo.L_Opening", "DocumentsID", c => c.Int(nullable: false));
			AddColumn("dbo.L_ResearchProposal", "DocumentsID", c => c.Int(nullable: false));

			AddColumn("dbo.DocumentContainer", "Temp", c => c.Int()); // Temporary Column

			// Neue Dokumentencontainer für jeden vorhandenen Listeneintrag anlegen
			Sql(@"INSERT INTO [dbo].[DocumentContainer] (Temp, AclID) SELECT ID, AclID FROM [dbo].[L_Event]");
			Sql(@"UPDATE event
					SET DocumentsID = (SELECT ID FROM [dbo].[DocumentContainer] as dc WHERE dc.Temp = event.ID)
					FROM [dbo].[L_Event] event");
			Sql(@"UPDATE [dbo].[DocumentContainer] SET Temp = NULL");

			Sql(@"INSERT INTO [dbo].[DocumentContainer] (Temp, AclID) SELECT ID, AclID FROM [dbo].[L_Holiday]");
			Sql(@"UPDATE item
					SET DocumentsID = (SELECT ID FROM [dbo].[DocumentContainer] as dc WHERE dc.Temp = item.ID)
					FROM [dbo].[L_Holiday] item");
			Sql(@"UPDATE [dbo].[DocumentContainer] SET Temp = NULL");

			Sql(@"INSERT INTO [dbo].[DocumentContainer] (Temp, AclID) SELECT ID, AclID FROM [dbo].[L_IndustryProject]");
			Sql(@"UPDATE item
					SET DocumentsID = (SELECT ID FROM [dbo].[DocumentContainer] as dc WHERE dc.Temp = item.ID)
					FROM [dbo].[L_IndustryProject] item");
			Sql(@"UPDATE [dbo].[DocumentContainer] SET Temp = NULL");

			Sql(@"INSERT INTO [dbo].[DocumentContainer] (Temp, AclID) SELECT ID, AclID FROM [dbo].[L_Opening]");
			Sql(@"UPDATE item
					SET DocumentsID = (SELECT ID FROM [dbo].[DocumentContainer] as dc WHERE dc.Temp = item.ID)
					FROM [dbo].[L_Opening] item");
			Sql(@"UPDATE [dbo].[DocumentContainer] SET Temp = NULL");

			Sql(@"INSERT INTO [dbo].[DocumentContainer] (Temp, AclID) SELECT ID, AclID FROM [dbo].[L_ResearchProposal]");
			Sql(@"UPDATE item
					SET DocumentsID = (SELECT ID FROM [dbo].[DocumentContainer] as dc WHERE dc.Temp = item.ID)
					FROM [dbo].[L_ResearchProposal] item");
			Sql(@"UPDATE [dbo].[DocumentContainer] SET Temp = NULL");

			DropColumn("dbo.DocumentContainer", "Temp");

			CreateIndex("dbo.L_Event", "DocumentsID");
			CreateIndex("dbo.L_Holiday", "DocumentsID");
			CreateIndex("dbo.L_IndustryProject", "DocumentsID");
			CreateIndex("dbo.L_Opening", "DocumentsID");
			CreateIndex("dbo.L_ResearchProposal", "DocumentsID");
			AddForeignKey("dbo.L_Event", "DocumentsID", "dbo.DocumentContainer", "ID", cascadeDelete: true);
			AddForeignKey("dbo.L_Holiday", "DocumentsID", "dbo.DocumentContainer", "ID", cascadeDelete: true);
			AddForeignKey("dbo.L_IndustryProject", "DocumentsID", "dbo.DocumentContainer", "ID", cascadeDelete: true);
			AddForeignKey("dbo.L_Opening", "DocumentsID", "dbo.DocumentContainer", "ID", cascadeDelete: true);
			AddForeignKey("dbo.L_ResearchProposal", "DocumentsID", "dbo.DocumentContainer", "ID", cascadeDelete: true);
		}

		public override void Down()
		{
			DropForeignKey("dbo.L_ResearchProposal", "DocumentsID", "dbo.DocumentContainer");
			DropForeignKey("dbo.L_Opening", "DocumentsID", "dbo.DocumentContainer");
			DropForeignKey("dbo.L_IndustryProject", "DocumentsID", "dbo.DocumentContainer");
			DropForeignKey("dbo.L_Holiday", "DocumentsID", "dbo.DocumentContainer");
			DropForeignKey("dbo.L_Event", "DocumentsID", "dbo.DocumentContainer");
			DropIndex("dbo.L_ResearchProposal", new[] { "DocumentsID" });
			DropIndex("dbo.L_Opening", new[] { "DocumentsID" });
			DropIndex("dbo.L_IndustryProject", new[] { "DocumentsID" });
			DropIndex("dbo.L_Holiday", new[] { "DocumentsID" });
			DropIndex("dbo.L_Event", new[] { "DocumentsID" });
			DropColumn("dbo.L_ResearchProposal", "DocumentsID");
			DropColumn("dbo.L_Opening", "DocumentsID");
			DropColumn("dbo.L_IndustryProject", "DocumentsID");
			DropColumn("dbo.L_Holiday", "DocumentsID");
			DropColumn("dbo.L_Event", "DocumentsID");
		}
	}
}
