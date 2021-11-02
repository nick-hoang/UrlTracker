namespace InfoCaster.Umbraco.UrlTracker.Models
{
	public class UrlTrackerNodeModel
	{
		public int Id { get; set; }

		public string Url { get; set; }

		public string Name { get; set; }

		public UrlTrackerNodeModel Parent { get; set; }
	}
}
