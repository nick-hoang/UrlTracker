using System;
using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace InfoCaster.Umbraco.UrlTracker.Models
{
	[TableName("icUrlTracker")]
	[PrimaryKey("Id", AutoIncrement = true)]
	[ExplicitColumns]
	public class UrlTrackerSchema
	{
		[PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
		[Column("Id")]
		public int Id { get; set; }

		[Column("Culture")]
		[Length(10)]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public string Culture { get; set; }

		[Column("OldUrl")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public string OldUrl { get; set; }

		[Column("OldRegex")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public string OldRexEx { get; set; }

		[Column("RedirectRootNodeId")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public int? RedirectRootNodeId { get; set; }

		[Column("RedirectNodeId")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public int? RedirectNodeId { get; set; }

		[Column("RedirectUrl")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public string RedirectUrl { get; set; }

		[Column("RedirectHttpCode")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public int RedirectHttpCode { get; set; }

		[Column("RedirectPassThroughQueryString")]
		[Constraint(Default = "1")]
		public bool RedirectPassThroughQueryString { get; set; }

		[Column("ForceRedirect")]
		[Constraint(Default = "0")]
		public bool ForceRedirect { get; set; }

		[Column("Notes")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public string Notes { get; set; }

		[Column("Is404")]
		[Constraint(Default = false)]
		public bool Is404 { get; set; }

		[Column("Referrer")]
		[NullSetting(/*Could not decode attribute arguments.*/)]
		public string Referred { get; set; }

		[Column("Inserted")]
		[Constraint(Default = "getdate()")]
		public DateTime Inserted { get; set; }
	}
}
