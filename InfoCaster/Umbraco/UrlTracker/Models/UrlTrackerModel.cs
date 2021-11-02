using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using InfoCaster.Umbraco.UrlTracker.Helpers;
using InfoCaster.Umbraco.UrlTracker.Services;
using InfoCaster.Umbraco.UrlTracker.Settings;
using Newtonsoft.Json;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;

namespace InfoCaster.Umbraco.UrlTracker.Models
{
	[DebuggerDisplay("OUrl = {OldUrl} | Rgx = {OldRegex} | Qs = {OldUrlQuery} | Root = {RedirectRootNodeId}")]
	public class UrlTrackerModel
	{
		private IUrlTrackerHelper _urlTrackerHelper => DependencyResolverExtensions.GetService<IUrlTrackerHelper>(DependencyResolver.Current);

		private IUrlTrackerService _urlTrackerService => DependencyResolverExtensions.GetService<IUrlTrackerService>(DependencyResolver.Current);

		private IUrlTrackerSettings _urlTrackerSettings => DependencyResolverExtensions.GetService<IUrlTrackerSettings>(DependencyResolver.Current);

		private Lazy<string> _calculatedOldUrl => new Lazy<string>(delegate
		{
			if (!string.IsNullOrEmpty(CalculatedOldUrlWithDomain))
			{
				if (CalculatedOldUrlWithDomain.StartsWith("Regex:"))
				{
					return CalculatedOldUrlWithDomain;
				}
				string text = Uri.UnescapeDataString(new Uri(CalculatedOldUrlWithDomain).PathAndQuery);
				if (text.StartsWith("/"))
				{
					return text;
				}
				return "/" + text.Substring(1);
			}
			return string.Empty;
		});

		private Lazy<string> _calculatedOldUrlWithDomain => new Lazy<string>(delegate
		{
			try
			{
				if (string.IsNullOrEmpty(OldRegex) && string.IsNullOrEmpty(OldUrl))
				{
					throw new InvalidOperationException("Both OldRegex and OldUrl are empty, which is invalid. Please correct this by removing any entries where the OldUrl and OldRegex columns are empty.");
				}
				if (!string.IsNullOrEmpty(OldRegex) && string.IsNullOrEmpty(OldUrl))
				{
					return "Regex: " + OldRegex;
				}
				UrlTrackerDomain urlTrackerDomain = _urlTrackerService.GetDomains().FirstOrDefault((UrlTrackerDomain x) => x.NodeId == RedirectRootNodeId);
				if (urlTrackerDomain == null)
				{
					urlTrackerDomain = new UrlTrackerDomain
					{
						Id = -1,
						NodeId = RedirectRootNodeId,
						Name = HttpContext.Current.Request.Url.Host + ((HttpContext.Current.Request.Url.IsDefaultPort && !_urlTrackerSettings.AppendPortNumber()) ? string.Empty : (":" + HttpContext.Current.Request.Url.Port))
					};
				}
				Uri uri = new Uri(urlTrackerDomain.UrlWithDomain);
				string text = string.Format("{0}{1}{2}{3}", uri.Scheme, Uri.SchemeDelimiter, uri.Host, (uri.IsDefaultPort && !_urlTrackerSettings.AppendPortNumber()) ? string.Empty : (":" + uri.Port));
				if (_urlTrackerSettings.HasDomainOnChildNode())
				{
					return string.Format("{0}{1}{2}", new Uri(urlTrackerDomain.UrlWithDomain + ((!urlTrackerDomain.UrlWithDomain.EndsWith("/") && !OldUrl.StartsWith("/")) ? "/" : string.Empty) + _urlTrackerHelper.ResolveUmbracoUrl(OldUrl)), (!string.IsNullOrEmpty(OldUrlQuery)) ? "?" : string.Empty, OldUrlQuery);
				}
				return string.Format("{0}{1}{2}", new Uri(text + ((!text.EndsWith("/") && !OldUrl.StartsWith("/")) ? "/" : string.Empty) + _urlTrackerHelper.ResolveUmbracoUrl(OldUrl)), (!string.IsNullOrEmpty(OldUrlQuery)) ? "?" : string.Empty, OldUrlQuery);
			}
			catch (Exception)
			{
				return string.Empty;
			}
		});

		private Lazy<UrlTrackerNodeModel> _redirectRootNode => new Lazy<UrlTrackerNodeModel>(delegate
		{
			IPublishedContent val = _urlTrackerService.GetNodeById(RedirectRootNodeId);
			if (val != null)
			{
				if (val.Id == 0)
				{
					IPublishedContent val2 = _urlTrackerService.GetNodeById(-1).Children.FirstOrDefault();
					if (val2 != null && val2.Id > 0)
					{
						val = val2;
					}
				}
				UrlTrackerNodeModel obj = new UrlTrackerNodeModel
				{
					Id = RedirectRootNodeId,
					Url = _urlTrackerService.GetUrlByNodeId(RedirectRootNodeId, (!string.IsNullOrEmpty(Culture)) ? Culture : null),
					Name = val.Name
				};
				UrlTrackerNodeModel urlTrackerNodeModel = new UrlTrackerNodeModel();
				IPublishedContent parent = val.Parent;
				urlTrackerNodeModel.Id = ((parent != null) ? parent.Id : 0);
				IPublishedContent parent2 = val.Parent;
				urlTrackerNodeModel.Name = ((parent2 != null) ? parent2.Name : null) ?? "";
				IPublishedContent parent3 = val.Parent;
				urlTrackerNodeModel.Url = ((parent3 != null) ? PublishedContentExtensions.Url(parent3, (!string.IsNullOrEmpty(Culture)) ? Culture : null, (UrlMode)0) : null) ?? "";
				obj.Parent = urlTrackerNodeModel;
				return obj;
			}
			return null;
		});

		private Lazy<string> _redirectRootNodeName => new Lazy<string>(() => (RedirectNode != null) ? (_urlTrackerSettings.HasDomainOnChildNode() ? ((RedirectRootNode.Parent != null) ? (RedirectRootNode.Parent.Name + "/" + RedirectRootNode.Name) : RedirectRootNode.Name) : RedirectRootNode.Name) : string.Empty);

		private Lazy<UrlTrackerNodeModel> _redirectNode => new Lazy<UrlTrackerNodeModel>(delegate
		{
			if (RedirectNodeId.HasValue)
			{
				IPublishedContent nodeById = _urlTrackerService.GetNodeById(RedirectNodeId.Value);
				if (nodeById != null)
				{
					IPublishedContent val = null;
					try
					{
						val = nodeById.Parent;
					}
					catch
					{
					}
					return new UrlTrackerNodeModel
					{
						Id = nodeById.Id,
						Name = nodeById.Name,
						Url = _urlTrackerService.GetUrlByNodeId(nodeById.Id, (!string.IsNullOrEmpty(Culture)) ? Culture : null),
						Parent = new UrlTrackerNodeModel
						{
							Id = ((val != null) ? val.Id : 0),
							Name = (((val != null) ? val.Name : null) ?? ""),
							Url = ((val != null) ? _urlTrackerService.GetUrlByNodeId(val.Id, (!string.IsNullOrEmpty(Culture)) ? Culture : null) : "")
						}
					};
				}
			}
			return null;
		});

		private Lazy<string> _oldUrlQuery => new Lazy<string>(() => (OldUrl != null && OldUrl.Contains("?")) ? OldUrl.Substring(OldUrl.IndexOf('?') + 1) : string.Empty);

		private Lazy<string> _calculatedRedirectUrl => new Lazy<string>(delegate
		{
			string text = ((!string.IsNullOrEmpty(RedirectUrl)) ? RedirectUrl : null);
			if (!string.IsNullOrEmpty(text))
			{
				if (text.StartsWith(Uri.UriSchemeHttp + Uri.SchemeDelimiter) || text.StartsWith(Uri.UriSchemeHttps + Uri.SchemeDelimiter))
				{
					return text;
				}
				return _urlTrackerHelper.ResolveShortestUrl(text);
			}
			if (RedirectRootNode != null && RedirectNodeId.HasValue && _urlTrackerService.GetUrlByNodeId(RedirectRootNode.Id, Culture).EndsWith("#"))
			{
				List<UrlTrackerDomain> source = (from x in _urlTrackerService.GetDomains()
					where x.NodeId == RedirectRootNode.Id
					select x).ToList();
				List<string> list = source.Select((UrlTrackerDomain n) => new Uri(n.UrlWithDomain).Host).ToList();
				if (list.Count == 0)
				{
					return _urlTrackerHelper.ResolveShortestUrl(_urlTrackerService.GetUrlByNodeId(RedirectNodeId.Value, Culture));
				}
				Uri sourceUrl = new Uri(source.First().UrlWithDomain);
				Uri uri = new Uri(sourceUrl, _urlTrackerService.GetUrlByNodeId(RedirectNodeId.Value, Culture));
				if (!uri.Host.Equals(sourceUrl.Host, StringComparison.OrdinalIgnoreCase))
				{
					return uri.AbsoluteUri;
				}
				if (list.Any((string n) => n.Equals(sourceUrl.Host, StringComparison.OrdinalIgnoreCase)))
				{
					string urlByNodeId = _urlTrackerService.GetUrlByNodeId(RedirectNodeId.Value, Culture);
					if (urlByNodeId.StartsWith(Uri.UriSchemeHttp))
					{
						Uri uri2 = new Uri(urlByNodeId);
						return _urlTrackerHelper.ResolveShortestUrl(uri2.AbsolutePath + uri2.Fragment);
					}
					return _urlTrackerHelper.ResolveShortestUrl(urlByNodeId);
				}
				Uri uri3 = new Uri(sourceUrl, _urlTrackerService.GetUrlByNodeId(RedirectNodeId.Value, Culture));
				if (sourceUrl.Host != uri3.Host)
				{
					return _urlTrackerHelper.ResolveShortestUrl(uri3.AbsoluteUri);
				}
				return _urlTrackerHelper.ResolveShortestUrl(uri3.AbsolutePath + uri3.Fragment);
			}
			return RedirectNodeId.HasValue ? _urlTrackerHelper.ResolveShortestUrl(_urlTrackerService.GetUrlByNodeId(RedirectNodeId.Value)) : string.Empty;
		});

		private Lazy<string> _oldUrlWithoutQuery => new Lazy<string>(delegate
		{
			if (CalculatedOldUrl.StartsWith("Regex:"))
			{
				return CalculatedOldUrl;
			}
			return (!CalculatedOldUrl.Contains('?')) ? CalculatedOldUrl : CalculatedOldUrl.Substring(0, CalculatedOldUrl.IndexOf('?'));
		});

		[JsonIgnore]
		public string CalculatedOldUrl => _calculatedOldUrl.Value;

		[JsonIgnore]
		public string CalculatedOldUrlWithDomain => _calculatedOldUrlWithDomain.Value;

		[JsonIgnore]
		public UrlTrackerNodeModel RedirectRootNode => _redirectRootNode.Value;

		[JsonIgnore]
		public string RedirectRootNodeName => _redirectRootNodeName.Value;

		[JsonIgnore]
		public UrlTrackerNodeModel RedirectNode => _redirectNode.Value;

		[JsonIgnore]
		public string OldUrlQuery => _oldUrlQuery.Value;

		public string CalculatedRedirectUrl => _calculatedRedirectUrl.Value;

		public string OldUrlWithoutQuery => _oldUrlWithoutQuery.Value;

		public int Id { get; set; }

		public string Culture { get; set; }

		public string OldUrl { get; set; }

		public string OldRegex { get; set; }

		public int RedirectRootNodeId { get; set; }

		public int? RedirectNodeId { get; set; }

		public string RedirectUrl { get; set; }

		public int RedirectHttpCode { get; set; }

		public bool RedirectPassThroughQueryString { get; set; }

		public string Notes { get; set; }

		public bool Is404 { get; set; }

		public bool Remove404 { get; set; }

		public string Referrer { get; set; }

		public int? Occurrences { get; set; }

		public DateTime Inserted { get; set; }

		public bool ForceRedirect { get; set; }

		public int Priority { get; set; }
	}
}
