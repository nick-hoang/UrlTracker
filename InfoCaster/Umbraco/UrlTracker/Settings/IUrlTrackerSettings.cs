using System;
using System.Text.RegularExpressions;

namespace InfoCaster.Umbraco.UrlTracker.Settings
{
	public interface IUrlTrackerSettings
	{
		string GetTableName();

		string GetOldTableName();

		string GetHttpModuleCheck();

		bool IsSEOMetadataInstalled();

		string GetSEOMetadataPropertyName();

		bool IsDisabled();

		bool IsTrackingDisabled();

		bool LoggingEnabled();

		Regex[] GetRegexNotFoundUrlsToIgnore();

		bool IsNotFoundTrackingDisabled();

		bool AppendPortNumber();

		bool HasDomainOnChildNode();

		bool IsForcedRedirectCacheTimeoutEnabled();

		TimeSpan GetForcedRedirectCacheTimeoutSeconds();

		string GetReferrerToIgnore();
	}
}
