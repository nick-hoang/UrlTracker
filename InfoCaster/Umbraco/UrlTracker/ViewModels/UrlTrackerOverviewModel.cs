using System.Collections.Generic;
using InfoCaster.Umbraco.UrlTracker.Models;

namespace InfoCaster.Umbraco.UrlTracker.ViewModels
{
	public class UrlTrackerOverviewModel
	{
		public IEnumerable<UrlTrackerModel> Entries { get; set; }

		public int NumberOfEntries { get; set; }
	}
}
