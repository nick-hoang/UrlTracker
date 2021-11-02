using System;
using System.Web.Caching;
using Umbraco.Core.Cache;

namespace InfoCaster.Umbraco.UrlTracker.Services
{
	public class UrlTrackerCacheService : IUrlTrackerCacheService
	{
		private readonly IAppPolicyCache _runtimeCache;

		public UrlTrackerCacheService(AppCaches appCaches)
		{
			_runtimeCache = appCaches.RuntimeCache;
		}

		public T Get<T>(string key)
		{
			return AppCacheExtensions.GetCacheItem<T>((IAppCache)(object)_runtimeCache, key);
		}

		public void Set<T>(string key, T value, TimeSpan? timeout = null)
		{
			AppCacheExtensions.InsertCacheItem<T>(_runtimeCache, key, (Func<T>)(() => value), timeout, false, CacheItemPriority.Normal, (CacheItemRemovedCallback)null, (string[])null);
		}

		public void Clear(string key)
		{
			((IAppCache)_runtimeCache).ClearByKey(key);
		}
	}
}
