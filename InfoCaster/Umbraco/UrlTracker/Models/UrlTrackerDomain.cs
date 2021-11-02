using System;
using System.Web;
using System.Web.Mvc;
using InfoCaster.Umbraco.UrlTracker.Services;
using InfoCaster.Umbraco.UrlTracker.Settings;
using Umbraco.Core.Models.PublishedContent;

namespace InfoCaster.Umbraco.UrlTracker.Models
{
	public class UrlTrackerDomain
	{
		private IUrlTrackerService _urlTrackerService => DependencyResolverExtensions.GetService<IUrlTrackerService>(DependencyResolver.Current);

		private IUrlTrackerSettings _urlTrackerSettings => DependencyResolverExtensions.GetService<IUrlTrackerSettings>(DependencyResolver.Current);

		private Lazy<IPublishedContent> _node => new Lazy<IPublishedContent>(() => _urlTrackerService.GetNodeById(NodeId));

		private Lazy<string> _urlWithDomain => new Lazy<string>(delegate
		{
			if (_urlTrackerSettings.HasDomainOnChildNode() && Node != null && Node.Parent != null)
			{
				return Node.Url;
			}
			return Name.Contains(Uri.UriSchemeHttp) ? Name : $"{((HttpContext.Current != null) ? HttpContext.Current.Request.Url.Scheme : Uri.UriSchemeHttp)}{Uri.SchemeDelimiter}{Name}";
		});

		public IPublishedContent Node => _node.Value;

		public string UrlWithDomain => _urlWithDomain.Value;

		public int Id { get; set; }

		public int NodeId { get; set; }

		public string Name { get; set; }

		public string LanguageIsoCode { get; set; }

		public override string ToString()
		{
			return UrlWithDomain;
		}
	}
}
