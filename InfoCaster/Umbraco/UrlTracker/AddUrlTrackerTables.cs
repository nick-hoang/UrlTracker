using InfoCaster.Umbraco.UrlTracker.Models;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace InfoCaster.Umbraco.UrlTracker
{
	public class AddUrlTrackerTables : MigrationBase
	{
		public AddUrlTrackerTables(IMigrationContext context): base(context)
		{
		}

		public override void Migrate()
		{
			LoggerExtensions.Debug<AddUrlTrackerTables>(Logger, "Running migration {MigrationStep}", new object[1] { "AddUrlTrackerTables" });
			if (!TableExists("icUrlTracker"))
			{
				Create.Table<UrlTrackerSchema>(false).Do();
			}
			else
			{
				LoggerExtensions.Debug<AddUrlTrackerTables>(Logger, "The database table {DbTable} already exists, skipping", new object[1] { "icUrlTracker" });
			}
			if (!TableExists("icUrlTrackerIgnore404"))
			{
				Create.Table<UrlTrackerIgnore404Schema>(false).Do();
				return;
			}
			LoggerExtensions.Debug<AddUrlTrackerTables>(Logger, "The database table {DbTable} already exists, skipping", new object[1] { "icUrlTrackerIgnore404" });
		}
	}
}
