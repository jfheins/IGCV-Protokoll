namespace IGCV_Protokoll.Migrations
{
	using System;
	using System.Data.Entity.Migrations;

	public partial class added_doc_container : DbMigration
	{
		public override void Up()
		{
			DropForeignKey("dbo.Document", "TopicID", "dbo.Topic");
			DropForeignKey("dbo.Document", "EmployeePresentationID", "dbo.L_EmployeePresentation");
			DropIndex("dbo.Document", new[] { "TopicID" });
			DropIndex("dbo.Document", new[] { "EmployeePresentationID" });
			AddColumn("dbo.L_EmployeePresentation", "DocumentsID", c => c.Int(nullable: false));
			CreateTable(
				"dbo.DocumentContainer",
				c => new
				{
					ID = c.Int(nullable: false, identity: true),
					TopicID = c.Int(),
					Orphaned = c.DateTime(),
					AclID = c.Int(),
				})
				.PrimaryKey(t => t.ID)
				.ForeignKey("dbo.ACL", t => t.AclID)
				.ForeignKey("dbo.Topic", t => t.TopicID)
				.Index(t => t.AclID);

			Sql(@"CREATE UNIQUE NONCLUSTERED INDEX idx_TopicID_notnull
					ON [dbo].[DocumentContainer](TopicID)
					WHERE TopicID IS NOT NULL;");

			AddColumn("dbo.DocumentContainer", "EmpP", c => c.Int()); // Temporary Column

			AddColumn("dbo.Document", "ParentContainerID", c => c.Int(nullable: false));
			CreateIndex("dbo.Document", "ParentContainerID");
			CreateIndex("dbo.L_EmployeePresentation", "DocumentsID");

			// Dokumente im Papierkorb löschen
			Sql(@"DELETE FROM [dbo].[Document]
					WHERE (EmployeePresentationID IS NULL) AND (TopicID IS NULL)");

			// Dokumente von Diskussionen übertragen
			Sql(@"INSERT INTO [dbo].[DocumentContainer]
									([TopicID])
							SELECT DISTINCT TopicID
							FROM [dbo].Document
							WHERE TopicID IS NOT NULL");

			Sql(@"UPDATE dc 
					SET AclID = (SELECT AclID FROM [dbo].[Topic] t WHERE t.ID = dc.TopicID)
					FROM [dbo].[DocumentContainer] AS dc
					WHERE dc.TopicID IS NOT NULL");

			Sql(@"UPDATE doc
					SET ParentContainerID = (SELECT ID FROM [dbo].[DocumentContainer] as dc WHERE dc.TopicID = doc.TopicID)
					FROM [dbo].[Document] doc
					WHERE doc.TopicID IS NOT NULL");
			// Dokumente von Listeneinträgen übertragen
			Sql(@"INSERT INTO [dbo].[DocumentContainer]
									(EmpP)
							SELECT ID
							FROM [dbo].[L_EmployeePresentation]");
			Sql(@"UPDATE doc
					SET ParentContainerID = (SELECT ID FROM [dbo].[DocumentContainer] as dc WHERE dc.EmpP = doc.EmployeePresentationID)
					FROM [dbo].[Document] doc
					WHERE doc.EmployeePresentationID IS NOT NULL");

			Sql(@"UPDATE empPres
					SET DocumentsID = (SELECT ID FROM [dbo].[DocumentContainer] as dc WHERE dc.EmpP = empPres.ID)
					FROM [dbo].[L_EmployeePresentation] empPres");


			AddForeignKey("dbo.Document", "ParentContainerID", "dbo.DocumentContainer", "ID", cascadeDelete: true);
			AddForeignKey("dbo.L_EmployeePresentation", "DocumentsID", "dbo.DocumentContainer", "ID", cascadeDelete: true);

			DropColumn("dbo.DocumentContainer", "EmpP");
			DropColumn("dbo.Document", "TopicID");
			DropColumn("dbo.Document", "EmployeePresentationID");
		}

		public override void Down()
		{
			AddColumn("dbo.Document", "EmployeePresentationID", c => c.Int());
			AddColumn("dbo.Document", "TopicID", c => c.Int());
			DropForeignKey("dbo.L_EmployeePresentation", "DocumentsID", "dbo.DocumentContainer");
			DropForeignKey("dbo.DocumentContainer", "TopicID", "dbo.Topic");
			DropForeignKey("dbo.Document", "ParentContainerID", "dbo.DocumentContainer");
			DropForeignKey("dbo.DocumentContainer", "AclID", "dbo.ACL");
			DropIndex("dbo.L_EmployeePresentation", new[] { "DocumentsID" });
			DropIndex("dbo.Document", new[] { "ParentContainerID" });
			DropIndex("dbo.DocumentContainer", new[] { "AclID" });
			DropIndex("dbo.DocumentContainer", new[] { "TopicID" });
			DropColumn("dbo.Document", "ParentContainerID");
			DropTable("dbo.DocumentContainer");
			RenameColumn(table: "dbo.L_EmployeePresentation", name: "DocumentsID", newName: "EmployeePresentationID");
			CreateIndex("dbo.Document", "EmployeePresentationID");
			CreateIndex("dbo.Document", "TopicID");
			AddForeignKey("dbo.Document", "EmployeePresentationID", "dbo.L_EmployeePresentation", "ID");
			AddForeignKey("dbo.Document", "TopicID", "dbo.Topic", "ID");
		}
	}
}
