using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using InfoCaster.Umbraco.UrlTracker.Helpers;
using InfoCaster.Umbraco.UrlTracker.Models;
using InfoCaster.Umbraco.UrlTracker.Repositories;
using InfoCaster.Umbraco.UrlTracker.Settings;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;

namespace InfoCaster.Umbraco.UrlTracker.Services
{
	public class UrlTrackerService : IUrlTrackerService
	{
		private readonly IUmbracoContextFactory _umbracoContextFactory;

		private readonly IUrlTrackerCacheService _urlTrackerCacheService;

		private readonly IUrlTrackerSettings _urlTrackerSettings;

		private readonly IUrlTrackerHelper _urlTrackerHelper;

		private readonly IUrlTrackerRepository _urlTrackerRepository;

		private readonly IUrlTrackerLoggingHelper _urlTrackerLoggingHelper;

		private readonly IContentService _contentService;

		private readonly IDomainService _domainService;

		private readonly ILocalizationService _localizationService;

		private readonly string _forcedRedirectsCacheKey = "UrlTrackerForcedRedirects";

		private readonly string _urlTrackerDomainsCacheKey = "UrlTrackerDomains";

		public UrlTrackerService(IUmbracoContextFactory umbracoContextFactory, IUrlTrackerCacheService urlTrackerCacheService, IUrlTrackerSettings urlTrackerSettings, IDomainService domainService, IUrlTrackerHelper urlTrackerHelper, IUrlTrackerRepository urlTrackerRepository, IUrlTrackerLoggingHelper urlTrackerLoggingHelper, IContentService contentService, ILocalizationService localizationService)
		{
			_umbracoContextFactory = umbracoContextFactory;
			_urlTrackerCacheService = urlTrackerCacheService;
			_urlTrackerSettings = urlTrackerSettings;
			_domainService = domainService;
			_urlTrackerHelper = urlTrackerHelper;
			_urlTrackerRepository = urlTrackerRepository;
			_urlTrackerLoggingHelper = urlTrackerLoggingHelper;
			_contentService = contentService;
			_localizationService = localizationService;
		}

		public bool AddRedirect(UrlTrackerModel entry)
		{
			if (entry.Remove404)
			{
				_urlTrackerRepository.DeleteNotFounds(entry.Culture, entry.OldUrl, entry.RedirectRootNodeId);
			}
			entry.OldUrl = ((!string.IsNullOrEmpty(entry.OldUrl)) ? _urlTrackerHelper.ResolveShortestUrl(entry.OldUrl) : null);
			entry.RedirectUrl = ((!string.IsNullOrEmpty(entry.RedirectUrl)) ? _urlTrackerHelper.ResolveShortestUrl(entry.RedirectUrl) : null);
			entry.OldRegex = ((!string.IsNullOrEmpty(entry.OldRegex)) ? entry.OldRegex : null);
			if (entry.ForceRedirect)
			{
				ClearForcedRedirectsCache();
			}
			return _urlTrackerRepository.AddEntry(entry);
		}

		public bool AddRedirect(IContent newContent, IPublishedContent oldContent, UrlTrackerHttpCode redirectType, UrlTrackerReason reason, string culture = null, bool isChild = false)
		{
			string text = (string.IsNullOrEmpty(culture) ? oldContent.Url() : PublishedContentExtensions.Url(oldContent, culture, (UrlMode)0));
			if (text == "#" || newContent.TemplateId <= 0)
			{
				return false;
			}
			string urlWithoutDomain = "";
			GetUmbracoDomainFromUrl(text, ref urlWithoutDomain);
			int id = PublishedContentExtensions.Root(oldContent).Id;
			string text2 = _urlTrackerHelper.ResolveShortestUrl(urlWithoutDomain);
			if (text2 == "/" || string.IsNullOrEmpty(text2) || _urlTrackerRepository.RedirectExist(((IEntity)newContent).Id, text2, culture))
			{
				return false;
			}
			string text3 = (isChild ? "An ancestor" : "This page");
			switch (reason)
			{
			case UrlTrackerReason.Moved:
				text3 += " was moved";
				break;
			case UrlTrackerReason.Renamed:
				text3 += " was renamed";
				break;
			case UrlTrackerReason.UrlOverwritten:
				text3 += "'s property 'umbracoUrlName' changed";
				break;
			case UrlTrackerReason.UrlOverwrittenSEOMetadata:
				text3 = text3 + "'s property '" + _urlTrackerSettings.GetSEOMetadataPropertyName() + "' changed";
				break;
			}
			if (_urlTrackerSettings.HasDomainOnChildNode())
			{
				Uri uri = new Uri(GetUrlByNodeId(id));
				string text4 = _urlTrackerHelper.ResolveShortestUrl(uri.AbsolutePath);
				if (text2.StartsWith(text4, StringComparison.OrdinalIgnoreCase))
				{
					text2 = _urlTrackerHelper.ResolveShortestUrl(text2.Substring(text4.Length));
				}
			}
			_urlTrackerLoggingHelper.LogInformation("UrlTracker Repository | Adding mapping for node id: {0} and url: {1}", oldContent.Id.ToString(), text2);
			UrlTrackerModel urlTrackerModel = new UrlTrackerModel
			{
				Culture = ((!string.IsNullOrEmpty(culture)) ? culture : null),
				RedirectHttpCode = (int)redirectType,
				RedirectRootNodeId = id,
				RedirectNodeId = ((IEntity)newContent).Id,
				OldUrl = text2,
				Notes = text3
			};
			_urlTrackerRepository.AddEntry(urlTrackerModel);
			if (urlTrackerModel.ForceRedirect)
			{
				ClearForcedRedirectsCache();
			}
			foreach (IPublishedContent child in oldContent.Children)
			{
				AddRedirect(_contentService.GetById(child.Id), child, redirectType, reason, culture, isChild: true);
			}
			return true;
		}

		public bool AddNotFound(string url, int rootNodeId, string referrer, string culture = null)
		{
			return _urlTrackerRepository.AddEntry(new UrlTrackerModel
			{
				Culture = culture,
				RedirectRootNodeId = rootNodeId,
				OldUrl = url,
				Referrer = referrer,
				Is404 = true
			});
		}

		public UrlTrackerDomain GetUmbracoDomainFromUrl(string url, ref string urlWithoutDomain)
		{
			List<UrlTrackerDomain> domains = GetDomains();
			if (domains.Any())
			{
				string urlWithoutQuery = (url.Contains("?") ? url.Substring(0, url.IndexOf('?')) : url);
				urlWithoutQuery += ((!urlWithoutQuery.EndsWith("/")) ? "/" : "");
				while (!string.IsNullOrEmpty(urlWithoutQuery))
				{
					if (urlWithoutQuery.EndsWith("/"))
					{
						UrlTrackerDomain urlTrackerDomain = domains.FirstOrDefault((UrlTrackerDomain x) => x.UrlWithDomain == urlWithoutQuery || x.UrlWithDomain == urlWithoutQuery.TrimEnd('/') || x.UrlWithDomain == urlWithoutQuery.Replace("http", "https") || x.UrlWithDomain == urlWithoutQuery.Replace("https", "http"));
						if (urlTrackerDomain != null)
						{
							urlWithoutDomain = url.Replace(urlTrackerDomain.UrlWithDomain.Replace("http://", "https://"), "").Replace(urlTrackerDomain.UrlWithDomain.Replace("https://", "http://"), "");
							return urlTrackerDomain;
						}
					}
					urlWithoutQuery = urlWithoutQuery.Substring(0, urlWithoutQuery.Length - 1);
				}
			}
			urlWithoutDomain = url;
			return null;
		}

		public bool AddIgnore404(int id)
		{
			UrlTrackerModel entryById = _urlTrackerRepository.GetEntryById(id);
			if (entryById == null || !entryById.Is404)
			{
				return false;
			}
			bool num = _urlTrackerRepository.AddIgnore(new UrlTrackerIgnore404Schema
			{
				RootNodeId = entryById.RedirectRootNodeId,
				Culture = entryById.Culture,
				Url = entryById.OldUrl
			});
			if (num)
			{
				_urlTrackerRepository.DeleteNotFounds(entryById.Culture, entryById.OldUrl, entryById.RedirectRootNodeId);
			}
			return num;
		}

		public int ImportRedirects(HttpPostedFile file)
		{
			if (!file.FileName.EndsWith(".csv"))
			{
				throw new Exception("Is not a .csv file");
			}
			StreamReader streamReader = new StreamReader(file.InputStream);
			List<UrlTrackerModel> list = new List<UrlTrackerModel>();
			string text = "RootNodeId;Culture;Old URL;Regex;Redirect URL;Redirect node ID;Redirect HTTP Code;Forward query;Force redirect;Notes";
			int num = 1;
			while (!streamReader.EndOfStream)
			{
				string text2 = streamReader.ReadLine();
				string[] array = text2.Split(';');
				if (num == 1)
				{
					if (text2 == text || text2 == text.ToLower())
					{
						num++;
						continue;
					}
					throw new Exception("Columns are incorrect");
				}
				if (array.Count() != 10)
				{
					throw new Exception($"Values on line: {num} are incomplete");
				}
				int result = 0;
				bool result2 = false;
				bool result3 = false;
				string culture = array[1];
				string oldUrl = array[2];
				string oldRegex = array[3];
				string redirectUrl = array[4];
				string notes = array[9];
				if (!int.TryParse(array[0], out var result4))
				{
					throw new Exception($"'RootNodeId' on line: {num} is not an integer");
				}
				if (!string.IsNullOrWhiteSpace(array[5]) && !int.TryParse(array[5], out result))
				{
					throw new Exception($"'Redirect node ID' on line: {num} is not a integer");
				}
				if (!int.TryParse(array[6], out var result5) || (result5 != 301 && result5 != 302 && result5 != 410))
				{
					throw new Exception($"'Redirect HTTP Code' on line: {num} is invalid");
				}
				if (!string.IsNullOrWhiteSpace(array[7]) && !bool.TryParse(array[7], out result2))
				{
					throw new Exception($"'Forward query' on line: {num} is invalid");
				}
				if (!string.IsNullOrWhiteSpace(array[8]) && !bool.TryParse(array[8], out result3))
				{
					throw new Exception($"'Force redirect' on line: {num} is invalid");
				}
				UrlTrackerModel urlTrackerModel = new UrlTrackerModel
				{
					RedirectRootNodeId = result4,
					Culture = culture,
					OldUrl = oldUrl,
					OldRegex = oldRegex,
					RedirectUrl = redirectUrl,
					RedirectNodeId = ((result == 0) ? null : new int?(result)),
					RedirectHttpCode = result5,
					RedirectPassThroughQueryString = result2,
					ForceRedirect = result3,
					Notes = notes
				};
				if (!ValidateRedirect(urlTrackerModel))
				{
					throw new Exception($"Missing required values on line: {num}");
				}
				list.Add(urlTrackerModel);
				num++;
			}
			foreach (UrlTrackerModel item in list)
			{
				AddRedirect(item);
			}
			return list.Count;
		}

		public UrlTrackerModel GetEntryById(int id)
		{
			return _urlTrackerRepository.GetEntryById(id);
		}

		public UrlTrackerGetResult GetRedirects(int skip, int amount, UrlTrackerSortType sortType = UrlTrackerSortType.Default, string searchQuery = "")
		{
			return _urlTrackerRepository.GetRedirects(skip, amount, sortType, searchQuery);
		}

		public UrlTrackerGetResult GetNotFounds(int skip, int amount, UrlTrackerSortType sortType = UrlTrackerSortType.LastOccurredDesc, string searchQuery = "")
		{
			return _urlTrackerRepository.GetNotFounds(skip, amount, sortType, searchQuery);
		}

		public List<UrlTrackerModel> GetForcedRedirects()
		{
			List<UrlTrackerModel> list = _urlTrackerCacheService.Get<List<UrlTrackerModel>>(_forcedRedirectsCacheKey);
			if (list == null)
			{
				List<UrlTrackerModel> records = _urlTrackerRepository.GetRedirects(null, null, UrlTrackerSortType.Default, "", onlyForcedRedirects: true).Records;
				_urlTrackerCacheService.Set(_forcedRedirectsCacheKey, records, _urlTrackerSettings.IsForcedRedirectCacheTimeoutEnabled() ? new TimeSpan?(_urlTrackerSettings.GetForcedRedirectCacheTimeoutSeconds()) : null);
				return records;
			}
			return list;
		}

		public List<UrlTrackerDomain> GetDomains()
		{
			List<UrlTrackerDomain> list = _urlTrackerCacheService.Get<List<UrlTrackerDomain>>(_urlTrackerDomainsCacheKey);
			if (list == null)
			{
				IEnumerable<IDomain> all = _domainService.GetAll(_urlTrackerSettings.HasDomainOnChildNode());
				list = new List<UrlTrackerDomain>();
				foreach (IDomain item in all)
				{
					list.Add(new UrlTrackerDomain
					{
						Id = ((IEntity)item).Id,
						NodeId = item.RootContentId.Value,
						Name = item.DomainName,
						LanguageIsoCode = item.LanguageIsoCode
					});
				}
				list = list.OrderBy((UrlTrackerDomain x) => x.Name).ToList();
				_urlTrackerCacheService.Set(_urlTrackerDomainsCacheKey, list);
			}
			return list;
		}

		public string GetUrlByNodeId(int nodeId, string culture = "")
		{
			UmbracoContextReference val = _umbracoContextFactory.EnsureUmbracoContext((HttpContextBase)null);
			try
			{
				IPublishedContent byId = ((IPublishedCache)val.UmbracoContext.Content).GetById(nodeId);
				return (byId != null) ? PublishedContentExtensions.Url(byId, (!string.IsNullOrEmpty(culture)) ? culture : null, (UrlMode)0) : null;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public IPublishedContent GetNodeById(int nodeId)
		{
			UmbracoContextReference val = _umbracoContextFactory.EnsureUmbracoContext((HttpContextBase)null);
			try
			{
				return ((IPublishedCache)val.UmbracoContext.Content).GetById(nodeId);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public bool RedirectExist(int redirectNodeId, string oldUrl, string culture = "")
		{
			return _urlTrackerRepository.RedirectExist(redirectNodeId, oldUrl, culture);
		}

		public IEnumerable<UrlTrackerLanguage> GetLanguagesOutNodeDomains(int nodeId)
		{
			List<UrlTrackerLanguage> list = new List<UrlTrackerLanguage>();
			IEnumerable<ILanguage> allLanguages = _localizationService.GetAllLanguages();
			foreach (IDomain domain in _domainService.GetAssignedDomains(nodeId, _urlTrackerSettings.HasDomainOnChildNode()))
			{
				if (!list.Any((UrlTrackerLanguage x) => x.IsoCode == domain.LanguageIsoCode.ToLower()))
				{
					list.Add((from x in allLanguages
						where x.IsoCode == domain.LanguageIsoCode
						select new UrlTrackerLanguage
						{
							Id = ((IEntity)x).Id,
							IsoCode = x.IsoCode.ToLower(),
							CultureName = x.CultureName
						}).First());
				}
			}
			return list;
		}

		public int CountNotFoundsThisWeek()
		{
			DateTime start = DateTime.Now.Date.AddDays(0 - (DateTime.Now.DayOfWeek - 1));
			DateTime now = DateTime.Now;
			return _urlTrackerRepository.CountNotFoundsBetweenDates(start, now);
		}

		public bool IgnoreExist(string url, int RootNodeId, string culture)
		{
			return _urlTrackerRepository.IgnoreExist(url, RootNodeId, culture);
		}

		public string GetRedirectsCsv()
		{
			UrlTrackerGetResult redirects = _urlTrackerRepository.GetRedirects(null, null);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("RootNodeId;Culture;Old URL;Regex;Redirect URL;Redirect node ID;Redirect HTTP Code;Forward query;Force redirect;Priority;Notes");
			foreach (UrlTrackerModel record in redirects.Records)
			{
				stringBuilder.AppendLine($"{record.RedirectRootNodeId};{record.Culture};{record.OldUrl};{record.OldRegex};{record.RedirectUrl};{record.RedirectNodeId};{record.RedirectHttpCode};{record.RedirectPassThroughQueryString};{record.ForceRedirect};{record.Priority};{record.Notes}");
			}
			return stringBuilder.ToString();
		}

		public void UpdateEntry(UrlTrackerModel entry)
		{
			entry.OldUrl = ((!string.IsNullOrEmpty(entry.OldUrl)) ? _urlTrackerHelper.ResolveShortestUrl(entry.OldUrl) : null);
			entry.RedirectUrl = ((!string.IsNullOrEmpty(entry.RedirectUrl)) ? _urlTrackerHelper.ResolveShortestUrl(entry.RedirectUrl) : null);
			entry.OldRegex = ((!string.IsNullOrEmpty(entry.OldRegex)) ? entry.OldRegex : null);
			_urlTrackerRepository.UpdateEntry(entry);
			if (entry.ForceRedirect)
			{
				ClearForcedRedirectsCache();
			}
		}

		public void Convert410To301ByNodeId(int nodeId)
		{
			_urlTrackerRepository.Convert410To301ByNodeId(nodeId);
		}

		public void ConvertRedirectTo410ByNodeId(int nodeId)
		{
			_urlTrackerRepository.ConvertRedirectTo410ByNodeId(nodeId);
		}

		public void ClearDomains()
		{
			_urlTrackerCacheService.Clear(_urlTrackerDomainsCacheKey);
		}

		public void ClearForcedRedirectsCache()
		{
			_urlTrackerCacheService.Clear(_forcedRedirectsCacheKey);
		}

		public bool DeleteEntryById(int id, bool is404)
		{
			UrlTrackerModel entryById = _urlTrackerRepository.GetEntryById(id);
			if (entryById != null)
			{
				if (is404)
				{
					_urlTrackerRepository.DeleteNotFounds(entryById.Culture, entryById.OldUrl, entryById.RedirectRootNodeId);
				}
				_urlTrackerRepository.DeleteEntryById(id);
				if (entryById.ForceRedirect)
				{
					ClearForcedRedirectsCache();
				}
			}
			return true;
		}

		public void DeleteEntryByRedirectNodeId(int nodeId)
		{
			if (_urlTrackerRepository.DeleteEntryByRedirectNodeId(nodeId))
			{
				ClearForcedRedirectsCache();
			}
		}

		public bool ValidateRedirect(UrlTrackerModel redirect)
		{
			if ((string.IsNullOrEmpty(redirect.OldUrl) && string.IsNullOrEmpty(redirect.OldRegex)) || redirect.RedirectRootNodeId == 0 || ((!redirect.RedirectNodeId.HasValue || redirect.RedirectNodeId == 0) && string.IsNullOrEmpty(redirect.RedirectUrl)) || (redirect.RedirectHttpCode != 301 && redirect.RedirectHttpCode != 302 && redirect.RedirectHttpCode != 410))
			{
				return false;
			}
			return true;
		}
	}
}
