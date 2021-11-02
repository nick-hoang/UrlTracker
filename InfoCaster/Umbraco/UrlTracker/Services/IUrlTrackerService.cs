using System.Collections.Generic;
using System.Web;
using InfoCaster.Umbraco.UrlTracker.Models;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

namespace InfoCaster.Umbraco.UrlTracker.Services
{
	public interface IUrlTrackerService
	{
		bool AddRedirect(UrlTrackerModel entry);

		bool AddRedirect(IContent newContent, IPublishedContent oldContent, UrlTrackerHttpCode redirectType, UrlTrackerReason reason, string culture = null, bool isChild = false);

		bool AddNotFound(string url, int rootNodeId, string referrer, string culture = null);

		bool AddIgnore404(int id);

		int ImportRedirects(HttpPostedFile file);

		UrlTrackerModel GetEntryById(int id);

		UrlTrackerGetResult GetRedirects(int skip, int amount, UrlTrackerSortType sortType = UrlTrackerSortType.Default, string searchQuery = "");

		UrlTrackerGetResult GetNotFounds(int skip, int amount, UrlTrackerSortType sortType = UrlTrackerSortType.LastOccurredDesc, string searchQuery = "");

		List<UrlTrackerModel> GetForcedRedirects();

		List<UrlTrackerDomain> GetDomains();

		string GetUrlByNodeId(int nodeId, string culture = "");

		IPublishedContent GetNodeById(int nodeId);

		bool RedirectExist(int redirectNodeId, string oldUrl, string culture = "");

		IEnumerable<UrlTrackerLanguage> GetLanguagesOutNodeDomains(int nodeId);

		int CountNotFoundsThisWeek();

		UrlTrackerDomain GetUmbracoDomainFromUrl(string url, ref string urlWithoutDomain);

		bool IgnoreExist(string url, int RootNodeId, string culture);

		string GetRedirectsCsv();

		void UpdateEntry(UrlTrackerModel entry);

		void Convert410To301ByNodeId(int nodeId);

		void ConvertRedirectTo410ByNodeId(int nodeId);

		void ClearDomains();

		void ClearForcedRedirectsCache();

		bool DeleteEntryById(int id, bool is404 = false);

		void DeleteEntryByRedirectNodeId(int nodeId);

		bool ValidateRedirect(UrlTrackerModel redirect);
	}
}
