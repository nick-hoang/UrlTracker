using InfoCaster.Umbraco.UrlTracker.Models;
using System.Collections.Generic;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Persistence.SqlSyntax;

namespace InfoCaster.Umbraco.UrlTracker
{
	public class AddUrlTrackerTablesMigration : MigrationBase
	{
		public AddUrlTrackerTablesMigration(IMigrationContext context): base(context)
		{
		}

		public override void Migrate()
		{
			LoggerExtensions.Debug<AddUrlTrackerTablesMigration>(Logger, "Running migration {MigrationStep}", new object[1] { "AddUrlTrackerTables" });
			if (!TableExists("icUrlTracker"))
			{
				Create.Table<UrlTrackerSchema>(false).Do();								
				
			}
			else
			{
				//AddColumnIfNotExists<UrlTrackerSchema>(new List<ColumnInfo> { new ColumnInfo("icUrlTracker", "Priority", 14, "0", string.Empty, "int") }, "icUrlTracker", "Priority");
				//TODO: use the AddColumnIfNotExists function above instead of plain sql
				Database.Execute(@"
				IF COL_LENGTH('Priority', 'icUrlTracker') IS NULL
				   BEGIN
					  ALTER TABLE icUrlTracker
						ADD Priority int NOT NULL DEFAULT ('0')
				   END;
				");				
				LoggerExtensions.Debug<AddUrlTrackerTablesMigration>(Logger, "The database table {DbTable} already exists, skipping", new object[1] { "icUrlTracker" });
			}
			if (!TableExists("icUrlTrackerIgnore404"))
			{
				Create.Table<UrlTrackerIgnore404Schema>(false).Do();
				return;
			}
			LoggerExtensions.Debug<AddUrlTrackerTablesMigration>(Logger, "The database table {DbTable} already exists, skipping", new object[1] { "icUrlTrackerIgnore404" });
		}
	}
}
