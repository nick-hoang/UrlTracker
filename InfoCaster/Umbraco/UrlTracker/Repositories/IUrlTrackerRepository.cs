using System;
using InfoCaster.Umbraco.UrlTracker.Models;

namespace InfoCaster.Umbraco.UrlTracker.Repositories
{
	public interface IUrlTrackerRepository
	{
		bool AddEntry(UrlTrackerModel entry);

		bool AddIgnore(UrlTrackerIgnore404Schema ignore);

		T FirstOrDefault<T>(string query, object parameters = null);

		UrlTrackerModel GetEntryById(int id);

		UrlTrackerGetResult GetRedirects(int? skip, int? amount, UrlTrackerSortType sort = UrlTrackerSortType.Default, string searchQuery = "", bool onlyForcedRedirects = false);

		UrlTrackerGetResult GetNotFounds(int? skip, int? amount, UrlTrackerSortType sort = UrlTrackerSortType.LastOccurredDesc, string searchQuery = "");

		bool RedirectExist(int redirectNodeId, string oldUrl, string culture = "");

		bool IgnoreExist(string url, int rootNodeId, string culture);

		int CountNotFoundsBetweenDates(DateTime start, DateTime end);

		void Execute(string query, object parameters = null);

		void UpdateEntry(UrlTrackerModel entry);

		void Convert410To301ByNodeId(int nodeId);

		void ConvertRedirectTo410ByNodeId(int nodeId);

		void DeleteEntryById(int id);

		bool DeleteEntryByRedirectNodeId(int nodeId);

		void DeleteNotFounds(string culture, string url, int rootNodeId);

		bool DoesUrlTrackerTableExists();

		bool DoesUrlTrackerOldTableExists();
	}
}
