using System.Web.Hosting;
using InfoCaster.Umbraco.UrlTracker.Modules;
using InfoCaster.Umbraco.UrlTracker.Providers;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;

namespace InfoCaster.Umbraco.UrlTracker
{
	public class UrlTrackerPreApplicationStart
	{
		public static void PreApplicationStart()
		{
			DynamicModuleUtility.RegisterModule(typeof(UrlTrackerModule));
			HostingEnvironment.RegisterVirtualPathProvider(new EmbeddedResourcesVirtualPathProvider());
		}
	}
}
