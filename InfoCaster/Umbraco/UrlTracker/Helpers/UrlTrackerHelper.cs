using System;
using System.Collections.Generic;
using System.Linq;
using InfoCaster.Umbraco.UrlTracker.Services;
using Umbraco.Core.Configuration;
using Umbraco.Core.IO;

namespace InfoCaster.Umbraco.UrlTracker.Helpers
{
	public class UrlTrackerHelper : IUrlTrackerHelper
	{
		private readonly IUrlTrackerCacheService _urlTrackerCacheService;

		private readonly IGlobalSettings _globalSettings;

		private readonly string _reservedListCacheKey = "UrlTrackerReservedList";

		public UrlTrackerHelper(IUrlTrackerCacheService urlTrackerCacheService, IGlobalSettings globalSettings)
		{
			_urlTrackerCacheService = urlTrackerCacheService;
			_globalSettings = globalSettings;
		}

		public string ResolveShortestUrl(string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				return url;
			}
			if (url != "/")
			{
				if (url.StartsWith("/"))
				{
					url = url.Substring(1);
				}
				if (url.EndsWith("/"))
				{
					url = url.Substring(0, url.Length - "/".Length);
				}
			}
			return url;
		}

		public string ResolveUmbracoUrl(string url)
		{
			if (url.StartsWith("http://") || url.StartsWith("https://"))
			{
				url = Uri.UnescapeDataString(new Uri(url).PathAndQuery);
			}
			return url;
		}

		public bool IsReservedPathOrUrl(string url)
		{
			List<string> list = _urlTrackerCacheService.Get<List<string>>(_reservedListCacheKey);
			if (list == null)
			{
				list = new List<string>();
				string reservedUrls = _globalSettings.ReservedUrls;
				string reservedPaths = _globalSettings.ReservedPaths;
				string[] array = reservedUrls.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string text in array)
				{
					if (!string.IsNullOrWhiteSpace(text))
					{
						string text2 = IOHelper.ResolveUrl(text).Trim().ToLower();
						if (text2.Length > 0)
						{
							list.Add(text2);
						}
					}
				}
				array = reservedPaths.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string text3 in array)
				{
					if (!string.IsNullOrWhiteSpace(text3))
					{
						text3.EndsWith("/");
						string text4 = IOHelper.ResolveUrl(text3).Trim().ToLower();
						if (text4.Length > 0)
						{
							list.Add(text4 + (text4.EndsWith("/") ? "" : "/"));
						}
					}
				}
				_urlTrackerCacheService.Set(_reservedListCacheKey, list);
			}
			string pathPart = url.Split('?')[0];
			if (!pathPart.Contains(".") && !pathPart.EndsWith("/"))
			{
				pathPart += "/";
			}
			if (pathPart.Length > 1 && pathPart[0] != '/')
			{
				pathPart = "/" + pathPart;
			}
			return list.Any((string u) => u.StartsWith(pathPart.ToLowerInvariant()));
		}
	}
}
