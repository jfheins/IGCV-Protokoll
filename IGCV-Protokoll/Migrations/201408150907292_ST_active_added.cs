using System.Data.Entity.Migrations;

namespace IGCV_Protokoll.Migrations
{
	public partial class ST_active_added : DbMigration
	{
		public override void Up()
		{
			AddColumn("dbo.SessionType", "Active", c => c.Boolean(nullable: false, defaultValue: true));
		}

		public override void Down()
		{
			DropColumn("dbo.SessionType", "Active");
		}
	}
}