using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace InfoCaster.Umbraco.UrlTracker.Settings
{
	public class UrlTrackerSettings : IUrlTrackerSettings
	{
		private string TableName => "icUrlTracker";

		private string OldTableName => "infocaster301";

		private string HttpModuleCheck => "890B748D-E226-49FA-A0D7-9AFD3A359F88";

		private Lazy<bool> _seoMetadataInstalled => new Lazy<bool>(() => AppDomain.CurrentDomain.GetAssemblies().Any((Assembly x) => x.FullName.Contains("Epiphany.SeoMetadata")));

		private Lazy<string> _seoMetadataPropertyName => new Lazy<string>(() => string.IsNullOrEmpty(ConfigurationManager.AppSettings["SeoMetadata.PropertyName"]) ? "metadata" : ConfigurationManager.AppSettings["SeoMetadata.PropertyName"]);

		private Lazy<bool> _isDisabled => new Lazy<bool>(delegate
		{
			if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["urlTracker:disabled"]))
			{
				bool.TryParse(ConfigurationManager.AppSettings["urlTracker:disabled"], out var result);
				return result;
			}
			return false;
		});

		private Lazy<bool> _enableLogging
		{
			get
			{
				bool result;
				return new Lazy<bool>(() => !string.IsNullOrEmpty(ConfigurationManager.AppSettings["urlTracker:enableLogging"]) && bool.TryParse(ConfigurationManager.AppSettings["urlTracker:enableLogging"], out result) && result);
			}
		}

		private Lazy<Regex[]> _regexNotFoundUrlsToIgnore => new Lazy<Regex[]>(() => new Regex[2]
		{
			new Regex("__browserLink/requestData/.*", RegexOptions.Compiled),
			new Regex("[^/]/arterySignalR/ping", RegexOptions.Compiled)
		});

		private Lazy<bool> _isTrackingDisabled
		{
			get
			{
				bool result;
				return new Lazy<bool>(() => !string.IsNullOrEmpty(ConfigurationManager.AppSettings["urlTracker:trackingDisabled"]) && bool.TryParse(ConfigurationManager.AppSettings["urlTracker:trackingDisabled"], out result) && result);
			}
		}

		private Lazy<bool> _isNotFoundTrackingDisabled
		{
			get
			{
				bool result;
				return new Lazy<bool>(() => !string.IsNullOrEmpty(ConfigurationManager.AppSettings["urlTracker:notFoundTrackingDisabled"]) && bool.TryParse(ConfigurationManager.AppSettings["urlTracker:notFoundTrackingDisabled"], out result) && result);
			}
		}

		private Lazy<bool> _appendPortNumber
		{
			get
			{
				bool result;
				return new Lazy<bool>(() => string.IsNullOrEmpty(ConfigurationManager.AppSettings["urlTracker:appendPortNumber"]) || !bool.TryParse(ConfigurationManager.AppSettings["urlTracker:appendPortNumber"], out result) || result);
			}
		}

		private Lazy<bool> _hasDomainOnChildNode
		{
			get
			{
				bool result;
				return new Lazy<bool>(() => !string.IsNullOrEmpty(ConfigurationManager.AppSettings["urlTracker:hasDomainOnChildNode"]) && bool.TryParse(ConfigurationManager.AppSettings["urlTracker:hasDomainOnChildNode"], out result) && result);
			}
		}

		private Lazy<bool> _forcedRedirectCacheTimeoutEnabled
		{
			get
			{
				bool result;
				return new Lazy<bool>(() => !string.IsNullOrEmpty(ConfigurationManager.AppSettings["urlTracker:forcedRedirectCacheTimeoutEnabled"]) && bool.TryParse(ConfigurationManager.AppSettings["urlTracker:forcedRedirectCacheTimeoutEnabled"], out result) && result);
			}
		}

		private Lazy<TimeSpan> _forcedRedirectCacheTimeoutSeconds
		{
			get
			{
				int result;
				return new Lazy<TimeSpan>(() => (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["urlTracker:forcedRedirectCacheTimeoutSeconds"]) && int.TryParse(ConfigurationManager.AppSettings["urlTracker:forcedRedirectCacheTimeoutSeconds"], out result) && result > 0) ? new TimeSpan(0, 0, result) : new TimeSpan(0, 0, 14400));
			}
		}

		public string GetTableName()
		{
			return TableName;
		}

		public string GetOldTableName()
		{
			return OldTableName;
		}

		public string GetHttpModuleCheck()
		{
			return HttpModuleCheck;
		}

		public bool IsSEOMetadataInstalled()
		{
			return _seoMetadataInstalled.Value;
		}

		public string GetSEOMetadataPropertyName()
		{
			return _seoMetadataPropertyName.Value;
		}

		public bool IsDisabled()
		{
			return _isDisabled.Value;
		}

		public bool IsTrackingDisabled()
		{
			return _isTrackingDisabled.Value;
		}

		public bool LoggingEnabled()
		{
			return _enableLogging.Value;
		}

		public Regex[] GetRegexNotFoundUrlsToIgnore()
		{
			return _regexNotFoundUrlsToIgnore.Value;
		}

		public bool IsNotFoundTrackingDisabled()
		{
			return _isNotFoundTrackingDisabled.Value;
		}

		public bool AppendPortNumber()
		{
			return _appendPortNumber.Value;
		}

		public bool HasDomainOnChildNode()
		{
			return _hasDomainOnChildNode.Value;
		}

		public bool IsForcedRedirectCacheTimeoutEnabled()
		{
			return _forcedRedirectCacheTimeoutEnabled.Value;
		}

		public TimeSpan GetForcedRedirectCacheTimeoutSeconds()
		{
			return _forcedRedirectCacheTimeoutSeconds.Value;
		}

		public string GetReferrerToIgnore()
		{
			return "Umbraco/UrlTracker/InfoCaster.Umbraco.UrlTracker.UI";
		}
	}
}
