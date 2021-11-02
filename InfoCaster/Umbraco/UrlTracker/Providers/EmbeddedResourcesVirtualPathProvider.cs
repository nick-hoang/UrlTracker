using System;
using System.Collections;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;

namespace InfoCaster.Umbraco.UrlTracker.Providers
{
	public class EmbeddedResourcesVirtualPathProvider : VirtualPathProvider
	{
		private bool IsEmbeddedResourcePath(string virtualPath)
		{
			return VirtualPathUtility.ToAppRelative(virtualPath).StartsWith("~/Umbraco/UrlTracker/", StringComparison.InvariantCultureIgnoreCase);
		}

		public override bool FileExists(string virtualPath)
		{
			if (!IsEmbeddedResourcePath(virtualPath))
			{
				return base.FileExists(virtualPath);
			}
			return true;
		}

		public override VirtualFile GetFile(string virtualPath)
		{
			if (IsEmbeddedResourcePath(virtualPath))
			{
				return new EmbeddedResourceVirtualFile(virtualPath);
			}
			return base.GetFile(virtualPath);
		}

		public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
		{
			if (IsEmbeddedResourcePath(virtualPath))
			{
				return null;
			}
			return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
		}
	}
}
