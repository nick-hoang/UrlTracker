using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace InfoCaster.Umbraco.UrlTracker.Helpers
{
	public static class ContextHelper
	{
		private class HttpContextEnsurer : IDisposable
		{
			private readonly bool _fake;

			private const string _tempUri = "http://tempuri.org";

			private static bool _umbracoContextTypeChecked;

			private static Type _umbracoContextType;

			private static Type _applicationContextType;

			private static PropertyInfo _umbracoContextCurrentProperty;

			private static MethodInfo _ensureContextMethodInfo;

			public HttpContextEnsurer()
			{
				_fake = HttpContext.Current == null;
				if (_fake)
				{
					HttpContext.Current = new HttpContext(new HttpRequest(string.Empty, "http://tempuri.org", string.Empty), new HttpResponse(new StringWriter()));
				}
				if (!_umbracoContextTypeChecked)
				{
					_umbracoContextType = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
						where assembly.GetName().Name == "umbraco"
						from type in assembly.GetTypes()
						where type.Name == "UmbracoContext" && type.Namespace == "Umbraco.Web"
						select type).FirstOrDefault();
					_umbracoContextTypeChecked = true;
				}
				if (!(_umbracoContextType != null))
				{
					return;
				}
				if (_umbracoContextCurrentProperty == null)
				{
					_umbracoContextCurrentProperty = _umbracoContextType.GetProperty("Current");
				}
				if (_umbracoContextCurrentProperty.GetValue(null, null) != null)
				{
					return;
				}
				if (_applicationContextType == null)
				{
					_applicationContextType = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
						where assembly.GetName().Name == "Umbraco.Core"
						from type in assembly.GetTypes()
						where type.Name == "ApplicationContext"
						select type).FirstOrDefault();
				}
				if (_ensureContextMethodInfo == null)
				{
					_ensureContextMethodInfo = _umbracoContextType.GetMethod("EnsureContext", new Type[3]
					{
						typeof(HttpContextBase),
						_applicationContextType,
						typeof(bool)
					});
				}
				if (_ensureContextMethodInfo != null)
				{
					_ensureContextMethodInfo.Invoke(null, new object[3]
					{
						new HttpContextWrapper(HttpContext.Current),
						_applicationContextType.GetProperty("Current").GetValue(null, null),
						true
					});
				}
			}

			public void Dispose()
			{
				if (_fake)
				{
					HttpContext.Current = null;
				}
			}
		}

		public static IDisposable EnsureHttpContext()
		{
			return new HttpContextEnsurer();
		}
	}
}
