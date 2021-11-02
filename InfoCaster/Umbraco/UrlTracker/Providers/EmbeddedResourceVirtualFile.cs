using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;

namespace InfoCaster.Umbraco.UrlTracker.Providers
{
	public class EmbeddedResourceVirtualFile : VirtualFile
	{
		private readonly string _path;

		public EmbeddedResourceVirtualFile(string virtualPath)
			: base(virtualPath)
		{
			_path = VirtualPathUtility.ToAppRelative(virtualPath);
		}

		public override Stream Open()
		{
			string resourceName = _path.Split('/').Last();
			Assembly assembly = GetType().Assembly;
			resourceName = assembly.GetManifestResourceNames().SingleOrDefault((string x) => x.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
			if (string.IsNullOrEmpty(resourceName))
			{
				return null;
			}
			return assembly.GetManifestResourceStream(resourceName);
		}
	}
}
