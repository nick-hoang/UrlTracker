using System;
using System.Data;
using System.Text;
using InfoCaster.Umbraco.UrlTracker.Helpers;
using InfoCaster.Umbraco.UrlTracker.Models;
using InfoCaster.Umbraco.UrlTracker.Services;
using InfoCaster.Umbraco.UrlTracker.Settings;
using NPoco;
using Umbraco.Core.Events;
using Umbraco.Core.Scoping;

namespace InfoCaster.Umbraco.UrlTracker.Repositories
{
	public class UrlTrackerRepository : IUrlTrackerRepository
	{
		private readonly IScopeProvider _scopeProvider;

		private readonly IUrlTrackerCacheService _urlTrackerCacheService;

		private readonly IUrlTrackerSettings _urlTrackerSettings;

		private readonly IUrlTrackerLoggingHelper _urlTrackerLoggingHelper;

		private readonly string _forcedRedirectsCacheKey = "UrlTrackerForcedRedirects";

		public UrlTrackerRepository(IScopeProvider scopeProvider, IUrlTrackerCacheService urlTrackerCacheService, IUrlTrackerSettings urlTrackerSettings, IUrlTrackerLoggingHelper urlTrackerLoggingHelper)
		{
			_scopeProvider = scopeProvider;
			_urlTrackerCacheService = urlTrackerCacheService;
			_urlTrackerSettings = urlTrackerSettings;
			_urlTrackerLoggingHelper = urlTrackerLoggingHelper;
		}

		public bool AddEntry(UrlTrackerModel entry)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				string text = "INSERT INTO icUrlTracker (Culture, OldUrl, OldRegex, RedirectRootNodeId, RedirectNodeId, RedirectUrl, RedirectHttpCode, RedirectPassThroughQueryString, ForceRedirect, Notes, Is404, Referrer) VALUES (@culture, @oldUrl, @oldRegex, @redirectRootNodeId, @redirectNodeId, @redirectUrl, @redirectHttpCode, @redirectPassThroughQueryString, @forceRedirect, @notes, @is404, @referrer)";
				var anon = new
				{
					culture = entry.Culture?.ToLower(),
					oldUrl = entry.OldUrl,
					oldRegex = entry.OldRegex,
					redirectRootNodeId = entry.RedirectRootNodeId,
					redirectNodeId = entry.RedirectNodeId,
					redirectUrl = entry.RedirectUrl,
					redirectHttpCode = entry.RedirectHttpCode,
					redirectPassThroughQueryString = entry.RedirectPassThroughQueryString,
					forceRedirect = entry.ForceRedirect,
					notes = entry.Notes,
					is404 = entry.Is404,
					referrer = entry.Referrer
				};
				return ((IDatabaseQuery)val.Database).Execute(text, new object[1] { anon }) == 1;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public bool AddIgnore(UrlTrackerIgnore404Schema ignore)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				string text = "INSERT INTO icUrlTrackerIgnore404 (RootNodeId, Culture, Url) VALUES (@rootNodeId, @culture, @url)";
				var anon = new
				{
					rootNodeId = ignore.RootNodeId,
					culture = ignore.Culture?.ToLower(),
					url = ignore.Url
				};
				return ((IDatabaseQuery)val.Database).Execute(text, new object[1] { anon }) == 1;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public T FirstOrDefault<T>(string query, object parameters = null)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				return ((IDatabaseQuery)val.Database).FirstOrDefault<T>(query, new object[1] { parameters });
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public UrlTrackerModel GetEntryById(int id)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				return ((IDatabaseQuery)val.Database).SingleOrDefault<UrlTrackerModel>("SELECT * FROM icUrlTracker WHERE Id = @id", new object[1]
				{
					new { id }
				});
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public UrlTrackerGetResult GetRedirects(int? skip, int? amount, UrlTrackerSortType sort = UrlTrackerSortType.CreatedDesc, string searchQuery = "", bool onlyForcedRedirects = false)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				UrlTrackerGetResult urlTrackerGetResult = new UrlTrackerGetResult();
				int result = 0;
				StringBuilder stringBuilder = new StringBuilder("SELECT COUNT(*) FROM icUrlTracker WHERE is404 = 0");
				if (!string.IsNullOrEmpty(searchQuery))
				{
					stringBuilder.Append(" AND (OldUrl LIKE @searchQuery OR OldRegex LIKE @searchQuery OR RedirectUrl LIKE @searchQuery OR Notes LIKE @searchQuery");
					if (int.TryParse(searchQuery, out result))
					{
						stringBuilder.Append(" OR RedirectNodeId = @searchQueryInt");
					}
					stringBuilder.Append(")");
				}
				if (onlyForcedRedirects)
				{
					stringBuilder.Append(" AND ForceRedirect = 1");
				}
				var anon = new
				{
					skip = skip,
					amount = amount,
					searchQuery = "%" + searchQuery + "%",
					searchQueryInt = result
				};
				urlTrackerGetResult.TotalRecords = ((IDatabaseQuery)val.Database).ExecuteScalar<int>(stringBuilder.ToString(), new object[1] { anon });
				switch (sort)
				{
				case UrlTrackerSortType.CreatedDesc:
					stringBuilder.Append(" ORDER BY Inserted DESC");
					break;
				case UrlTrackerSortType.CreatedAsc:
					stringBuilder.Append(" ORDER BY Inserted ASC");
					break;
				case UrlTrackerSortType.CultureDesc:
					stringBuilder.Append(" ORDER BY Culture DESC");
					break;
				case UrlTrackerSortType.CultureAsc:
					stringBuilder.Append(" ORDER BY Culture ASC");
					break;
				}
				stringBuilder.Replace("SELECT COUNT(*)", "SELECT *");
				if (skip.HasValue)
				{
					stringBuilder.Append(" OFFSET @skip ROWS");
				}
				if (amount.HasValue)
				{
					stringBuilder.Append(" FETCH NEXT @amount ROWS ONLY");
				}
				urlTrackerGetResult.Records = ((IDatabaseQuery)val.Database).Fetch<UrlTrackerModel>(stringBuilder.ToString(), new object[1] { anon });
				return urlTrackerGetResult;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public UrlTrackerGetResult GetNotFounds(int? skip, int? amount, UrlTrackerSortType sort = UrlTrackerSortType.LastOccurredDesc, string searchQuery = "")
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				UrlTrackerGetResult urlTrackerGetResult = new UrlTrackerGetResult();
				StringBuilder stringBuilder = new StringBuilder("SELECT COUNT(*) FROM (SELECT e.OldUrl FROM icUrlTracker AS e WHERE e.Is404 = 1");
				if (!string.IsNullOrEmpty(searchQuery))
				{
					stringBuilder.Append(" AND (e.OldUrl LIKE @searchQuery)");
				}
				stringBuilder.Append(" GROUP BY e.Culture, e.OldUrl, e.RedirectRootNodeId, e.Is404) result");
				var anon = new
				{
					skip = skip,
					amount = amount,
					searchQuery = "%" + searchQuery + "%"
				};
				urlTrackerGetResult.TotalRecords = ((IDatabaseQuery)val.Database).ExecuteScalar<int>(stringBuilder.ToString(), new object[1] { anon });
				switch (sort)
				{
				case UrlTrackerSortType.LastOccurredDesc:
					stringBuilder.Append(" ORDER BY Inserted DESC");
					break;
				case UrlTrackerSortType.LastOccurredAsc:
					stringBuilder.Append(" ORDER BY Inserted ASC");
					break;
				case UrlTrackerSortType.OccurrencesDesc:
					stringBuilder.Append(" ORDER BY Occurrences DESC");
					break;
				case UrlTrackerSortType.OccurrencesAsc:
					stringBuilder.Append(" ORDER BY Occurrences ASC");
					break;
				case UrlTrackerSortType.CultureDesc:
					stringBuilder.Append(" ORDER BY Culture DESC");
					break;
				case UrlTrackerSortType.CultureAsc:
					stringBuilder.Append(" ORDER BY Culture ASC");
					break;
				}
				StringBuilder stringBuilder2 = new StringBuilder("SELECT * FROM (SELECT MAX(e.Id) AS Id, e.Culture, e.OldUrl, e.RedirectRootNodeId, MAX(e.Inserted) as Inserted, COUNT(e.OldUrl) AS Occurrences, e.Is404");
				stringBuilder2.Append(", Referrer = (SELECT TOP(1) r.Referrer AS Occurrenced FROM icUrlTracker AS r WHERE r.is404 = 1 AND r.OldUrl = e.OldUrl GROUP BY r.Referrer ORDER BY COUNT(r.Referrer) DESC)");
				stringBuilder.Replace("SELECT COUNT(*) FROM (SELECT e.OldUrl", stringBuilder2.ToString());
				if (skip.HasValue)
				{
					stringBuilder.Append(" OFFSET @skip ROWS");
				}
				if (amount.HasValue)
				{
					stringBuilder.Append(" FETCH NEXT @amount ROWS ONLY");
				}
				urlTrackerGetResult.Records = ((IDatabaseQuery)val.Database).Fetch<UrlTrackerModel>(stringBuilder.ToString(), new object[1] { anon });
				return urlTrackerGetResult;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public bool RedirectExist(int redirectNodeId, string oldUrl, string culture)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				return ((IDatabaseQuery)val.Database).ExecuteScalar<bool>("SELECT 1 FROM icUrlTracker WHERE RedirectNodeId = @redirectNodeId AND OldUrl = @oldUrl AND Culture = @culture", new object[1]
				{
					new
					{
						redirectNodeId = redirectNodeId,
						oldUrl = oldUrl,
						culture = culture?.ToLower()
					}
				});
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public bool IgnoreExist(string url, int rootNodeId, string culture)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				return ((IDatabaseQuery)val.Database).ExecuteScalar<bool>("SELECT 1 FROM icUrlTrackerIgnore404 WHERE Url = @url AND RootNodeId = @rootNodeId AND (Culture = @culture OR Culture IS NULL)", new object[1]
				{
					new
					{
						url = url,
						rootNodeId = rootNodeId,
						culture = culture?.ToLower()
					}
				});
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public int CountNotFoundsBetweenDates(DateTime start, DateTime end)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				return ((IDatabaseQuery)val.Database).ExecuteScalar<int>("SELECT COUNT(*) FROM (SELECT OldUrl FROM icUrlTracker WHERE Is404 = 1 AND Inserted BETWEEN @start AND @end GROUP BY Culture, OldUrl, RedirectRootNodeId, Is404) result", new object[1]
				{
					new { start, end }
				});
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public void Execute(string query, object parameters = null)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				((IDatabaseQuery)val.Database).Execute(query, new object[1] { parameters });
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public void UpdateEntry(UrlTrackerModel entry)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, (RepositoryCacheMode)0, (IEventDispatcher)null, (bool?)null, false, true);
			try
			{
				string text = "UPDATE icUrlTracker SET Culture = @culture, OldUrl = @oldUrl, OldRegex = @oldRegex, RedirectRootNodeId = @redirectRootNodeId, RedirectNodeId = @redirectNodeId, RedirectUrl = @redirectUrl, RedirectHttpCode = @redirectHttpCode, RedirectPassThroughQueryString = @redirectPassThroughQueryString, ForceRedirect = @forceRedirect, Notes = @notes, Is404 = @is404 WHERE Id = @id";
				var anon = new
				{
					culture = entry.Culture?.ToLower(),
					oldUrl = entry.OldUrl,
					oldRegex = entry.OldRegex,
					redirectRootNodeId = entry.RedirectRootNodeId,
					redirectNodeId = entry.RedirectNodeId,
					redirectUrl = entry.RedirectUrl,
					redirectHttpCode = entry.RedirectHttpCode,
					redirectPassThroughQueryString = entry.RedirectPassThroughQueryString,
					forceRedirect = entry.ForceRedirect,
					notes = entry.Notes,
					is404 = entry.Is404,
					id = entry.Id
				};
				((IDatabaseQuery)val.Database).Execute(text, new object[1] { anon });
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public void Convert410To301ByNodeId(int nodeId)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, RepositoryCacheMode.Unspecified, null, null, false, true);
			try
			{
				((IDatabaseQuery)val.Database).Execute("UPDATE icUrlTracker SET RedirectHttpCode = 301 WHERE RedirectHttpCode = 410 AND RedirectNodeId = @redirectNodeId", new object[1]
				{
					new
					{
						redirectNodeId = nodeId
					}
				});
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public void ConvertRedirectTo410ByNodeId(int nodeId)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, RepositoryCacheMode.Unspecified, null, null, false, true);
			try
			{
				((IDatabaseQuery)val.Database).Execute("UPDATE icUrlTracker SET RedirectHttpCode = 410 WHERE (RedirectHttpCode = 301 OR RedirectHttpCode = 302) AND RedirectNodeId = @redirectNodeId", new object[1]
				{
					new
					{
						redirectNodeId = nodeId
					}
				});
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public void DeleteEntryById(int id)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, RepositoryCacheMode.Unspecified, null, (bool?)null, false, true);
			try
			{
				((IDatabaseQuery)val.Database).Execute("DELETE FROM icUrlTracker WHERE Id = @id", new object[1]
				{
					new { id }
				});
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public bool DeleteEntryByRedirectNodeId(int nodeId)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, RepositoryCacheMode.Unspecified, null, (bool?)null, false, true);
			try
			{
				if (((IDatabaseQuery)val.Database).ExecuteScalar<int>("SELECT 1 FROM icUrlTracker WHERE RedirectNodeId = @nodeId AND RedirectHttpCode != 410", new object[1]
				{
					new { nodeId }
				}) == 1)
				{
					_urlTrackerLoggingHelper.LogInformation("UrlTracker Repository | Deleting Url Tracker entry of node with id: {0}", nodeId);
					((IDatabaseQuery)val.Database).Execute("DELETE FROM icUrlTracker WHERE RedirectNodeId = @nodeId AND RedirectHttpCode != 410", new object[1]
					{
						new { nodeId }
					});
					return true;
				}
				return false;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public void DeleteNotFounds(string culture, string url, int rootNodeId)
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, RepositoryCacheMode.Unspecified, null, null, false, true);
			try
			{
				string text = "DELETE FROM icUrlTracker WHERE Culture = @culture AND OldUrl = @url AND RedirectRootNodeId = @rootNodeId AND Is404 = 1";
				if (string.IsNullOrEmpty(culture))
				{
					text = text.Replace("Culture = @culture", "Culture IS NULL");
				}
				((IDatabaseQuery)val.Database).Execute(text, new object[1]
				{
					new
					{
						culture = culture?.ToLower(),
						url = url,
						rootNodeId = rootNodeId
					}
				});
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public bool DoesUrlTrackerTableExists()
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, RepositoryCacheMode.Unspecified, null, null, false, true);
			try
			{
				return ((IDatabaseQuery)val.Database).ExecuteScalar<int>("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'icUrlTracker'", Array.Empty<object>()) == 1;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public bool DoesUrlTrackerOldTableExists()
		{
			IScope val = _scopeProvider.CreateScope(IsolationLevel.Unspecified, RepositoryCacheMode.Unspecified, null, null, false, true);
			try
			{
				return ((IDatabaseQuery)val.Database).ExecuteScalar<int>("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName", new object[1]
				{
					new
					{
						tableName = _urlTrackerSettings.GetOldTableName()
					}
				}) == 1;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}
}
