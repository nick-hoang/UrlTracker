using System;
using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace InfoCaster.Umbraco.UrlTracker.Models
{
	[TableName("icUrlTrackerIgnore404")]
	[PrimaryKey("Id", AutoIncrement = true)]
	[ExplicitColumns]
	public class UrlTrackerIgnore404Schema
	{
		[PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
		[Column("Id")]
		public int Id { get; set; }

		[Column("RootNodeId")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public int? RootNodeId { get; set; }

		[Column("Culture")]
		[Length(10)]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public string Culture { get; set; }

		[Column("Url")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public string Url { get; set; }

		[Column("Inserted")]
		[Constraint(Default = "getdate()")]
		public DateTime Inserted { get; set; }
	}
}
