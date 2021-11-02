using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;
using InfoCaster.Umbraco.UrlTracker.Models;
using InfoCaster.Umbraco.UrlTracker.Services;
using InfoCaster.Umbraco.UrlTracker.Settings;
using InfoCaster.Umbraco.UrlTracker.ViewModels;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace InfoCaster.Umbraco.UrlTracker.Controllers
{
	[PluginController("UrlTracker")]
	public class UrlTrackerManagerController : UmbracoAuthorizedApiController
	{
		private readonly IUrlTrackerService _urlTrackerService;

		private readonly IUrlTrackerSettings _urlTrackerSettings;

		public UrlTrackerManagerController(IUrlTrackerService urlTrackerService, IUrlTrackerSettings urlTrackerSettings)
			: this()
		{
			_urlTrackerService = urlTrackerService;
			_urlTrackerSettings = urlTrackerSettings;
		}

        public UrlTrackerManagerController()
        {
        }

        [HttpPost]
		public IHttpActionResult DeleteEntry(int id, bool is404 = false)
		{
			if (!_urlTrackerService.DeleteEntryById(id, is404))
			{
				return BadRequest();
			}
            return Ok();
		}

		[HttpGet]
		public IHttpActionResult GetLanguagesOutNodeDomains(int nodeId)
		{
			return Ok<IEnumerable<UrlTrackerLanguage>>(_urlTrackerService.GetLanguagesOutNodeDomains(nodeId));
		}

		[HttpGet]
		public IHttpActionResult GetSettings()
		{
			return Ok(new
			{
				IsDisabled = _urlTrackerSettings.IsDisabled(),
				EnableLogging = _urlTrackerSettings.LoggingEnabled(),
				TrackingDisabled = _urlTrackerSettings.IsTrackingDisabled(),
				IsNotFoundTrackingDisabled = _urlTrackerSettings.IsNotFoundTrackingDisabled(),
				AppendPortNumber = _urlTrackerSettings.AppendPortNumber()
			});
		}

		[HttpPost]
		public IHttpActionResult AddRedirect([FromBody] UrlTrackerModel model)
		{
			if (!_urlTrackerService.ValidateRedirect(model))
			{
				return BadRequest("Not all fields are filled in correctly");
			}
			_urlTrackerService.AddRedirect(model);
			return Ok();
		}

		[HttpPost]
		public IHttpActionResult UpdateRedirect([FromBody] UrlTrackerModel model)
		{
			if (!_urlTrackerService.ValidateRedirect(model))
			{
				return BadRequest("Not all fields are filled in correctly");
			}
			_urlTrackerService.UpdateEntry(model);
			return Ok();
		}

		[HttpGet]
		public IHttpActionResult GetRedirects(int skip, int amount, string query = "", UrlTrackerSortType sortType = UrlTrackerSortType.CreatedDesc)
		{
			UrlTrackerGetResult redirects = _urlTrackerService.GetRedirects(skip, amount, sortType, query);
			UrlTrackerOverviewModel urlTrackerOverviewModel = new UrlTrackerOverviewModel
			{
				Entries = redirects.Records,
				NumberOfEntries = redirects.TotalRecords
			};
			return Ok<UrlTrackerOverviewModel>(urlTrackerOverviewModel);
		}

		[HttpGet]
		public IHttpActionResult GetNotFounds(int skip, int amount, string query = "", UrlTrackerSortType sortType = UrlTrackerSortType.LastOccurredDesc)
		{
			UrlTrackerGetResult notFounds = _urlTrackerService.GetNotFounds(skip, amount, sortType, query);
			UrlTrackerOverviewModel urlTrackerOverviewModel = new UrlTrackerOverviewModel
			{
				Entries = notFounds.Records,
				NumberOfEntries = notFounds.TotalRecords
			};
			return Ok<UrlTrackerOverviewModel>(urlTrackerOverviewModel);
		}

		[HttpGet]
		public IHttpActionResult CountNotFoundsThisWeek()
		{
			return Ok<int>(_urlTrackerService.CountNotFoundsThisWeek());
		}

		[HttpPost]
		public IHttpActionResult AddIgnore404([FromBody] int id)
		{
			if (!_urlTrackerService.AddIgnore404(id))
			{
				return BadRequest();
			}
			return Ok();
		}

		[HttpPost]
		public IHttpActionResult ImportRedirects()
		{
			HttpPostedFile httpPostedFile = ((HttpContext.Current.Request.Files.Count > 0) ? HttpContext.Current.Request.Files[0] : null);
			if (httpPostedFile == null || !httpPostedFile.FileName.EndsWith(".csv"))
			{
				return BadRequest("A .csv file is required");
			}
			try
			{
				int num = _urlTrackerService.ImportRedirects(httpPostedFile);
				return Ok<int>(num);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet]
		public HttpResponseMessage ExportRedirects()
		{			
			string redirectsCsv = _urlTrackerService.GetRedirectsCsv();
			HttpResponseMessage obj = Request.CreateResponse(HttpStatusCode.OK);
			obj.Content = (HttpContent)new StringContent(redirectsCsv, Encoding.UTF8, "text/csv");
			HttpContentHeaders headers = obj.Content.Headers;
			ContentDispositionHeaderValue val = new ContentDispositionHeaderValue("attachment");
			val.FileName = $"urltracker-redirects-{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}.csv";
			headers.ContentDisposition = val;
			return obj;
		}
	}
}
