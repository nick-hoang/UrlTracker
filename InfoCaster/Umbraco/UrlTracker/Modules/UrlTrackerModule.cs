using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using InfoCaster.Umbraco.UrlTracker.Extensions;
using InfoCaster.Umbraco.UrlTracker.Helpers;
using InfoCaster.Umbraco.UrlTracker.Models;
using InfoCaster.Umbraco.UrlTracker.Repositories;
using InfoCaster.Umbraco.UrlTracker.Services;
using InfoCaster.Umbraco.UrlTracker.Settings;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Composing;
using Umbraco.Web.PublishedCache;
using Umbraco.Web.Routing;

namespace InfoCaster.Umbraco.UrlTracker.Modules
{
	public class UrlTrackerModule : IHttpModule
	{
		private static Regex _capturingGroupsRegex = new Regex("\\$\\d+");

		private static bool _urlTrackerInstalled;

		private static bool _urlTrackerSubscribed = false;

		private static readonly object _urlTrackerSubscribeLock = new object();

		private IRuntimeState _runtime => DependencyResolverExtensions.GetService<IRuntimeState>(DependencyResolver.Current);

		private IUrlTrackerHelper _urlTrackerHelper => DependencyResolverExtensions.GetService<IUrlTrackerHelper>(DependencyResolver.Current);

		private IUrlTrackerService _urlTrackerService => DependencyResolverExtensions.GetService<IUrlTrackerService>(DependencyResolver.Current);

		private IUrlTrackerSettings _urlTrackerSettings => DependencyResolverExtensions.GetService<IUrlTrackerSettings>(DependencyResolver.Current);

		private IUrlTrackerRepository _urlTrackerRepository => DependencyResolverExtensions.GetService<IUrlTrackerRepository>(DependencyResolver.Current);

		private IUrlTrackerLoggingHelper _urlTrackerLoggingHelper => DependencyResolverExtensions.GetService<IUrlTrackerLoggingHelper>(DependencyResolver.Current);

		public static event EventHandler<HttpResponse> PreUrlTracker;

		public void Dispose()
		{
		}

		public void Init(HttpApplication app)
		{
            //IL_000e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0014: Invalid comparison between Unknown and I4
            if (_runtime == null || (int)_runtime.Level != 4)
			{
				return;
			}
			_urlTrackerInstalled = true;
			if (!_urlTrackerSubscribed)
			{
				lock (_urlTrackerSubscribeLock)
				{
					UmbracoModule.EndRequest += UmbracoModule_EndRequest;
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Subscribed to EndRequest events");
					_urlTrackerSubscribed = true;
				}
			}
			app.PostResolveRequestCache -= Context_PostResolveRequestCache;
			app.PostResolveRequestCache += Context_PostResolveRequestCache;
			_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Subscribed to AcquireRequestState events");
		}

		private void Context_PostResolveRequestCache(object sender, EventArgs e)
		{
			try
			{
				UrlTrackerDo("AcquireRequestState", ignoreHttpStatusCode: true, HttpContext.Current);
			}
			catch (Exception ex)
			{
				_urlTrackerLoggingHelper.LogException(ex);
			}
		}

		private void UmbracoModule_EndRequest(object sender, UmbracoRequestEventArgs args)
		{
			try
			{
				UrlTrackerDo("EndRequest", ignoreHttpStatusCode: false, args.HttpContext.ApplicationInstance.Context);
			}
			catch (Exception ex)
			{
				_urlTrackerLoggingHelper.LogException(new Exception(ex.Message + " " + args.HttpContext.ApplicationInstance.Context.Request.Url.AbsoluteUri.ToString(), ex));
			}
		}

		private void UrlTrackerDo(string callingEventName, bool ignoreHttpStatusCode = false, HttpContext context = null)
		{			
			if (_urlTrackerSettings.IsDisabled())
			{
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | UrlTracker is disabled by config");
				return;
			}
			if (context == null)
			{
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | No HttpContext has been passed by {0}", callingEventName);
				context = HttpContext.Current;
			}
			HttpRequest request = context.Request;
			HttpResponse response = context.Response;
			if (!string.IsNullOrEmpty(request.QueryString[_urlTrackerSettings.GetHttpModuleCheck()]))
			{
				response.Clear();
				response.Write(_urlTrackerSettings.GetHttpModuleCheck());
				response.StatusCode = 200;				
				//response.End(); -> ThreadAbortException: https://docs.microsoft.com/en-us/troubleshoot/aspnet/threadabortexception-occurs-you-use-response-end
				context.ApplicationInstance.CompleteRequest();
				return;
			}
			_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | {0} start", callingEventName);
			_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Incoming URL is: {0}", _urlTrackerHelper.ResolveShortestUrl(request.RawUrl));
			if (_urlTrackerInstalled && (response.StatusCode == 404 || ignoreHttpStatusCode))
			{
				if (response.StatusCode == 404)
				{
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Response statusCode is 404, continue URL matching");
				}
				else
				{
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Checking for forced redirects (AcquireRequestState), continue URL matching");
				}
				string urlWithoutDomain = "";
				UrlTrackerDomain domain = _urlTrackerService.GetUmbracoDomainFromUrl(request.Url.ToString(), ref urlWithoutDomain);
				string shortestUrl = _urlTrackerHelper.ResolveShortestUrl(urlWithoutDomain);
				int rootNodeId = -1;
				bool flag = shortestUrl.Contains('?');
				string text = (flag ? shortestUrl.Substring(0, shortestUrl.IndexOf('?')) : shortestUrl);
				if (_urlTrackerHelper.IsReservedPathOrUrl(shortestUrl))
				{
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | URL is an umbraco reserved path or url, ignore request");
					return;
				}
				if (domain != null)
				{
					rootNodeId = domain.NodeId;
				}
				else if (Current.UmbracoContext != null)
				{
					IPublishedContent obj = Current.UmbracoContext.Content.GetAtRoot().FirstOrDefault();
					int? obj2;
					if (obj == null)
					{
						obj2 = null;
					}
					else
					{
						IPublishedContent obj3 = obj.Root();
						obj2 = ((obj3 != null) ? new int?(obj3.Id) : null);
					}
					rootNodeId = obj2 ?? (-1);
				}
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Current request's rootNodeId: {0}", rootNodeId);
				string redirectUrl = null;
				int? redirectHttpCode = null;
				bool redirectPassThroughQueryString = true;
				if (!ignoreHttpStatusCode)
				{
					LoadUrlTrackerMatchesFromDatabase(request, domain, shortestUrl, text, flag, rootNodeId, ref redirectUrl, ref redirectHttpCode, ref redirectPassThroughQueryString);
				}
				else
				{
					LoadUrlTrackerMatchesFromCache(request, domain, shortestUrl, text, flag, rootNodeId, ref redirectUrl, ref redirectHttpCode, ref redirectPassThroughQueryString);
				}
				if (!redirectHttpCode.HasValue)
				{
					if (!ignoreHttpStatusCode)
					{
						string query = "SELECT * FROM icUrlTracker WHERE Is404 = 0 AND ForceRedirect = @forceRedirect AND (RedirectRootNodeId = @redirectRootNodeId OR RedirectRootNodeId = -1) AND OldRegex IS NOT NULL ORDER BY Priority,Inserted DESC";
						UrlTrackerModel urlTrackerModel = _urlTrackerRepository.FirstOrDefault<UrlTrackerModel>(query, new
						{
							forceRedirect = (ignoreHttpStatusCode ? 1 : 0),
							redirectRootNodeId = rootNodeId
						});
						if (urlTrackerModel != null)
						{
							Regex regex = new Regex(urlTrackerModel.OldRegex);
							if (regex.IsMatch(shortestUrl))
							{
								_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Regex match found");
								if (urlTrackerModel.RedirectNodeId.HasValue)
								{
									int value = urlTrackerModel.RedirectNodeId.Value;
									_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect node id: {0}", value);
									IPublishedContent nodeById = _urlTrackerService.GetNodeById(rootNodeId);
									if (nodeById != null && nodeById.Name != null && nodeById.Id > 0)
									{
										redirectUrl = _urlTrackerService.GetUrlByNodeId(value);
										_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url set to: {0}", redirectUrl);
									}
									else
									{
										_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect node is invalid; node is null, name is null or id <= 0");
									}
								}
								else if (!string.IsNullOrWhiteSpace(urlTrackerModel.RedirectUrl))
								{
									redirectUrl = urlTrackerModel.RedirectUrl;
									_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url set to: {0}", redirectUrl);
									if (_capturingGroupsRegex.IsMatch(redirectUrl))
									{
										_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Found regex capturing groups in the redirect url");
										redirectUrl = regex.Replace(shortestUrl, redirectUrl);
										_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url changed to: {0} (because of regex capturing groups)", redirectUrl);
									}
								}
								redirectPassThroughQueryString = urlTrackerModel.RedirectPassThroughQueryString;
								_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | PassThroughQueryString is enabled");
								redirectHttpCode = urlTrackerModel.RedirectHttpCode;
								_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect http code set to: {0}", redirectHttpCode);
							}
						}
					}
					else
					{
						List<UrlTrackerModel> list = (from x in _urlTrackerService.GetForcedRedirects()
							where !string.IsNullOrEmpty(x.OldRegex)
							select x).ToList();
						if (list == null || !list.Any())
						{
							return;
						}
						foreach (var item in from x in list
							where (x.Culture == (domain?.LanguageIsoCode?.ToLower() ?? "") || x.Culture == null) && (x.RedirectRootNodeId == -1 || x.RedirectRootNodeId == rootNodeId)
							select new
							{
								UrlTrackerModel = x,
								Regex = new Regex(x.OldRegex)
							} into x
							where x.Regex.IsMatch(shortestUrl)
							select x)
						{
							_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Regex match found");
							if (item.UrlTrackerModel.RedirectNodeId.HasValue)
							{
								_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect node id: {0}", item.UrlTrackerModel.RedirectNodeId.Value);
								IPublishedContent nodeById2 = _urlTrackerService.GetNodeById(rootNodeId);
								if (nodeById2 != null && nodeById2.Name != null && nodeById2.Id > 0)
								{
									redirectUrl = _urlTrackerService.GetUrlByNodeId(item.UrlTrackerModel.RedirectNodeId.Value);
									_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url set to: {0}", redirectUrl);
								}
								else
								{
									_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect node is invalid; node is null, name is null or id <= 0");
								}
							}
							else if (!string.IsNullOrEmpty(item.UrlTrackerModel.RedirectUrl))
							{
								redirectUrl = item.UrlTrackerModel.RedirectUrl;
								_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url set to: {0}", redirectUrl);
								if (_capturingGroupsRegex.IsMatch(redirectUrl))
								{
									_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Found regex capturing groups in the redirect url");
									redirectUrl = item.Regex.Replace(shortestUrl, redirectUrl);
									_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url changed to: {0} (because of regex capturing groups)", redirectUrl);
								}
							}
							redirectPassThroughQueryString = item.UrlTrackerModel.RedirectPassThroughQueryString;
							_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | PassThroughQueryString is enabled");
							redirectHttpCode = item.UrlTrackerModel.RedirectHttpCode;
							_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect http code set to: {0}", redirectHttpCode);
						}
					}
				}
				if (redirectHttpCode.HasValue)
				{
					string text2 = string.Empty;
					if (!string.IsNullOrEmpty(redirectUrl))
					{
						if (redirectUrl == "/")
						{
							redirectUrl = string.Empty;
						}
						Uri uri = new Uri(redirectUrl.StartsWith(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ? redirectUrl : string.Format("{0}{1}{2}{3}/{4}", request.Url.Scheme, Uri.SchemeDelimiter, request.Url.Host, (request.Url.Port != 80 && _urlTrackerSettings.AppendPortNumber()) ? (":" + request.Url.Port) : string.Empty, redirectUrl.StartsWith("/") ? redirectUrl.Substring(1) : redirectUrl));
						if (redirectPassThroughQueryString)
						{
							NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
							NameValueCollection nameValueCollection2 = HttpUtility.ParseQueryString(request.Url.Query);
							if (nameValueCollection.HasKeys())
							{
								nameValueCollection2 = nameValueCollection2.Merge(nameValueCollection);
							}
							string text3 = Uri.UnescapeDataString(uri.PathAndQuery) + uri.Fragment;
							uri = new Uri(string.Format("{0}{1}{2}{3}/{4}{5}", uri.Scheme, Uri.SchemeDelimiter, uri.Host, (uri.Port != 80 && _urlTrackerSettings.AppendPortNumber()) ? (":" + uri.Port) : string.Empty, text3.Contains('?') ? text3.Substring(0, text3.IndexOf('?')) : (text3.StartsWith("/") ? text3.Substring(1) : text3), nameValueCollection2.HasKeys() ? ("?" + nameValueCollection2.ToQueryString()) : string.Empty));
						}
						if (uri == new Uri(string.Format("{0}{1}{2}{3}/{4}", request.Url.Scheme, Uri.SchemeDelimiter, request.Url.Host, (request.Url.Port != 80 && _urlTrackerSettings.AppendPortNumber()) ? (":" + request.Url.Port) : string.Empty, request.RawUrl.StartsWith("/") ? request.RawUrl.Substring(1) : request.RawUrl)))
						{
							_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect URL is the same as Request.RawUrl; don't redirect");
							return;
						}
						text2 = ((!request.Url.Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase)) ? uri.AbsoluteUri : (uri.PathAndQuery + uri.Fragment));
						_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Response redirectlocation set to: {0}", text2);
					}
					response.Clear();
					response.StatusCode = redirectHttpCode.Value;
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Response statuscode set to: {0}", response.StatusCode);
					if (UrlTrackerModule.PreUrlTracker != null)
					{
						UrlTrackerModule.PreUrlTracker(null, response);
						_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Custom event has been called: {0}", UrlTrackerModule.PreUrlTracker.Method.Name);
					}
					if (!string.IsNullOrEmpty(text2))
					{
						response.RedirectLocation = text2;
					}
					//response.End(); -> ThreadAbortException: https://docs.microsoft.com/en-us/troubleshoot/aspnet/threadabortexception-occurs-you-use-response-end
					context.ApplicationInstance.CompleteRequest();					
				}
				else if (!ignoreHttpStatusCode)
				{
					bool flag2 = _urlTrackerService.IgnoreExist(shortestUrl, rootNodeId, domain?.LanguageIsoCode);
					if (!flag2 && !_urlTrackerSettings.IsNotFoundTrackingDisabled() && !_urlTrackerHelper.IsReservedPathOrUrl(text) && request.Headers["X-UrlTracker-Ignore404"] != "1")
					{
						bool flag3 = false;
						Regex[] regexNotFoundUrlsToIgnore = _urlTrackerSettings.GetRegexNotFoundUrlsToIgnore();
						for (int i = 0; i < regexNotFoundUrlsToIgnore.Length; i++)
						{
							if (regexNotFoundUrlsToIgnore[i].IsMatch(text))
							{
								flag3 = true;
								break;
							}
						}
						if (!flag3)
						{
							_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | No match found, logging as 404 not found");
							_urlTrackerService.AddNotFound(shortestUrl, rootNodeId, (request.UrlReferrer != null && !request.UrlReferrer.ToString().Contains(_urlTrackerSettings.GetReferrerToIgnore())) ? request.UrlReferrer.ToString() : "", domain?.LanguageIsoCode);
						}
					}
					if (_urlTrackerSettings.IsNotFoundTrackingDisabled())
					{
						_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | No match found and not found (404) tracking is disabled");
					}
					else if (flag2)
					{
						_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | No match found, url is configured to be ignored: {0}", text);
					}
					else if (_urlTrackerHelper.IsReservedPathOrUrl(text))
					{
						_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | No match found, url is ignored because it's an umbraco reserved URL or path: {0}", text);
					}
					else if (request.Headers["X-UrlTracker-Ignore404"] == "1")
					{
						_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | No match found, url is ignored because the 'X-UrlTracker-Ignore404' header was set to '1'. URL: {0}", text);
					}
				}
				else
				{
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | No match found in {0}", callingEventName);
				}
			}
			else
			{
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Response statuscode is not 404, UrlTracker won't do anything");
			}
			_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | {0} end", callingEventName);
		}

		private void LoadUrlTrackerMatchesFromDatabase(HttpRequest request, UrlTrackerDomain domain, string shortestUrl, string urlWithoutQueryString, bool urlHasQueryString, int rootNodeId, ref string redirectUrl, ref int? redirectHttpCode, ref bool redirectPassThroughQueryString)
		{
			UrlTrackerModel urlTrackerModel = _urlTrackerRepository.FirstOrDefault<UrlTrackerModel>("SELECT * FROM icUrlTracker WHERE Is404 = 0 AND ForceRedirect = 0 AND (Culture = @culture OR Culture IS NULL) AND (RedirectRootNodeId = @redirectRootNodeId OR RedirectRootNodeId IS NULL OR RedirectRootNodeId = -1) AND (OldUrl = @urlWithoutQueryString OR OldUrl = @urlWithQueryString) ORDER BY CASE WHEN RedirectHttpCode = 410 THEN 2 ELSE 1 END", new
			{
				redirectRootNodeId = rootNodeId,
				urlWithoutQueryString = urlWithoutQueryString,
				urlWithQueryString = shortestUrl,
				culture = (domain?.LanguageIsoCode?.ToLower() ?? "")
			});
			if (urlTrackerModel == null)
			{
				return;
			}
			_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | URL match found");
			if (urlTrackerModel.RedirectNodeId.HasValue && urlTrackerModel.RedirectHttpCode != 410)
			{
				UrlTrackerNodeModel redirectRootNode = urlTrackerModel.RedirectRootNode;
				int value = urlTrackerModel.RedirectNodeId.Value;
				string culture = urlTrackerModel.Culture ?? "";
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect node id: {0}", value);
				if (redirectRootNode == null || redirectRootNode.Id <= 0)
				{
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect node is invalid; node is null, name is null or id <= 0");
					return;
				}
				string urlByNodeId = _urlTrackerService.GetUrlByNodeId(value, culture);
				redirectUrl = (urlByNodeId.StartsWith(Uri.UriSchemeHttp) ? urlByNodeId : string.Format("{0}{1}{2}{3}{4}", HttpContext.Current.Request.Url.Scheme, Uri.SchemeDelimiter, HttpContext.Current.Request.Url.Host, (HttpContext.Current.Request.Url.Port != 80 && _urlTrackerSettings.AppendPortNumber()) ? (":" + HttpContext.Current.Request.Url.Port) : string.Empty, urlByNodeId));
				if (redirectUrl.StartsWith(Uri.UriSchemeHttp))
				{
					Uri uri = new Uri(redirectUrl);
					string pathAndQuery = Uri.UnescapeDataString(uri.PathAndQuery) + uri.Fragment;
					redirectUrl = GetCorrectedUrl(uri, rootNodeId, pathAndQuery);
				}
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url set to: {0}", redirectUrl);
			}
			else if (!string.IsNullOrWhiteSpace(urlTrackerModel.RedirectUrl))
			{
				redirectUrl = urlTrackerModel.RedirectUrl;
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url set to: {0}", redirectUrl);
			}
			redirectPassThroughQueryString = urlTrackerModel.RedirectPassThroughQueryString;
			_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | PassThroughQueryString is {0}", redirectPassThroughQueryString ? "enabled" : "disabled");
			NameValueCollection nameValueCollection = null;
			if (!string.IsNullOrWhiteSpace(urlTrackerModel.OldUrlQuery))
			{
				nameValueCollection = HttpUtility.ParseQueryString(urlTrackerModel.OldUrlQuery);
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Old URL query string set to: {0}", nameValueCollection.ToQueryString());
			}
			if ((urlHasQueryString || nameValueCollection != null) && nameValueCollection != null && !request.QueryString.CollectionEquals(nameValueCollection))
			{
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Aborting; query strings don't match");
				return;
			}
			redirectHttpCode = urlTrackerModel.RedirectHttpCode;
			_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect http code set to: {0}", redirectHttpCode);
		}

		private void LoadUrlTrackerMatchesFromCache(HttpRequest request, UrlTrackerDomain domain, string shortestUrl, string urlWithoutQueryString, bool urlHasQueryString, int rootNodeId, ref string redirectUrl, ref int? redirectHttpCode, ref bool redirectPassThroughQueryString)
		{
			List<UrlTrackerModel> forcedRedirects = _urlTrackerService.GetForcedRedirects();
			if (forcedRedirects == null || !forcedRedirects.Any())
			{
				return;
			}
			foreach (UrlTrackerModel item in (from x in forcedRedirects
				where (x.Culture == (domain?.LanguageIsoCode?.ToLower() ?? "") || x.Culture == null) && (x.RedirectRootNodeId == rootNodeId || x.RedirectRootNodeId == -1) && (string.Equals(x.OldUrl ?? "", urlWithoutQueryString ?? "", StringComparison.CurrentCultureIgnoreCase) || string.Equals(x.OldUrl ?? "", shortestUrl ?? "", StringComparison.CurrentCultureIgnoreCase))
				orderby (x.RedirectHttpCode != 410) ? 1 : 2, x.OldUrlQuery descending
				select x).ToList())
			{
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | URL match found");
				if (item.RedirectNodeId.HasValue && item.RedirectHttpCode != 410)
				{
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect node id: {0}", item.RedirectNodeId.Value);
					if (item.RedirectNode == null || string.IsNullOrEmpty(item.RedirectNode.Name) || item.RedirectNode.Id <= 0)
					{
						_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect node is invalid; node is null, name is null or id <= 0");
						continue;
					}
					string culture = item.Culture ?? "";
					string urlByNodeId = _urlTrackerService.GetUrlByNodeId(item.RedirectNodeId.Value, culture);
					redirectUrl = (urlByNodeId.StartsWith(Uri.UriSchemeHttp) ? urlByNodeId : string.Format("{0}{1}{2}{3}{4}", HttpContext.Current.Request.Url.Scheme, Uri.SchemeDelimiter, HttpContext.Current.Request.Url.Host, (HttpContext.Current.Request.Url.Port != 80 && _urlTrackerSettings.AppendPortNumber()) ? (":" + HttpContext.Current.Request.Url.Port) : string.Empty, urlByNodeId));
					if (redirectUrl.StartsWith(Uri.UriSchemeHttp))
					{
						Uri uri = new Uri(redirectUrl);
						string pathAndQuery = Uri.UnescapeDataString(uri.PathAndQuery);
						redirectUrl = GetCorrectedUrl(uri, item.RedirectRootNodeId, pathAndQuery);
					}
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url set to: {0}", redirectUrl);
				}
				else if (!string.IsNullOrEmpty(item.RedirectUrl))
				{
					redirectUrl = item.RedirectUrl;
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect url set to: {0}", redirectUrl);
				}
				redirectPassThroughQueryString = item.RedirectPassThroughQueryString;
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | PassThroughQueryString is {0}", redirectPassThroughQueryString ? "enabled" : "disabled");
				NameValueCollection nameValueCollection = null;
				if (!string.IsNullOrEmpty(item.OldUrlQuery))
				{
					nameValueCollection = HttpUtility.ParseQueryString(item.OldUrlQuery);
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Old URL query string set to: {0}", nameValueCollection.ToQueryString());
				}
				if ((urlHasQueryString || nameValueCollection != null) && nameValueCollection != null && !request.QueryString.CollectionEquals(nameValueCollection))
				{
					_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Aborting; query strings don't match");
					continue;
				}
				redirectHttpCode = item.RedirectHttpCode;
				_urlTrackerLoggingHelper.LogInformation("UrlTracker HttpModule | Redirect http code set to: {0}", redirectHttpCode);
				break;
			}
		}

		private string GetCorrectedUrl(Uri redirectUri, int rootNodeId, string pathAndQuery)
		{
			string result = pathAndQuery;
			if (redirectUri.Host != HttpContext.Current.Request.Url.Host && !(from x in _urlTrackerService.GetDomains()
				where x.NodeId == rootNodeId
				select new Uri(x.UrlWithDomain).Host).ToList().Contains(redirectUri.Host))
			{
				result = new Uri(redirectUri, "/" + pathAndQuery.TrimStart('/')).AbsoluteUri;
			}
			return result;
		}
	}
}
