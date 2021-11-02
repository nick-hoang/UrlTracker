using System;
using System.Linq;
using InfoCaster.Umbraco.UrlTracker.Services;
using InfoCaster.Umbraco.UrlTracker.Settings;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Collections;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Migrations.Upgrade;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;

namespace InfoCaster.Umbraco.UrlTracker
{
	public class UrlTrackerComponent : IComponent
	{
		private readonly IScopeProvider _scopeProvider;

		private readonly IMigrationBuilder _migrationBuilder;

		private readonly IKeyValueService _keyValueService;

		private readonly ILogger _logger;

		private readonly IUrlTrackerService _urlTrackerService;

		private readonly IUrlTrackerSettings _urlTrackerSettings;

		public UrlTrackerComponent(IUrlTrackerSettings urlTrackerSettings, IUrlTrackerService urlTrackerService, IScopeProvider scopeProvider, IMigrationBuilder migrationBuilder, IKeyValueService keyValueService, ILogger logger)
		{
			_urlTrackerSettings = urlTrackerSettings;
			_scopeProvider = scopeProvider;
			_migrationBuilder = migrationBuilder;
			_keyValueService = keyValueService;
			_logger = logger;
			_urlTrackerService = urlTrackerService;
		}

		public void Initialize()
		{
			MigrationPlan val = new MigrationPlan("UrlTracker");
			val.From(string.Empty).To<AddUrlTrackerTablesMigration>("urlTracker");			
			new Upgrader(val).Execute(_scopeProvider, _migrationBuilder, _keyValueService, _logger);
			if (!_urlTrackerSettings.IsDisabled() && !_urlTrackerSettings.IsTrackingDisabled())
			{
				ContentService.Moving += ContentService_Moving;
				ContentService.Publishing += ContentService_Publishing;
				ContentService.Published += ContentService_Published;
				ContentService.Trashed += ContentService_Trashed;
				DomainService.Deleted += DomainService_Deleted;
				DomainService.Saved += DomainService_Saved;
			}
		}

		public void Terminate()
		{
		}

		private void DomainService_Saved(IDomainService sender, SaveEventArgs<IDomain> e)
		{
			_urlTrackerService.ClearDomains();
		}

		private void DomainService_Deleted(IDomainService sender, DeleteEventArgs<IDomain> e)
		{
			_urlTrackerService.ClearDomains();
		}

		private void ContentService_Trashed(IContentService sender, MoveEventArgs<IContent> e)
		{
			try
			{
				foreach (MoveEventInfo<IContent> item in e.MoveInfoCollection)
				{
					IContent entity = item.Entity;
					if (entity == null)
					{
						break;
					}
					_urlTrackerService.ConvertRedirectTo410ByNodeId(((IEntity)entity).Id);
				}
			}
			catch (Exception ex)
			{
				LoggerExtensions.Error<UrlTrackerComponent>(_logger, ex);
			}
		}

		private void ContentService_Publishing(IContentService sender, ContentPublishingEventArgs e)
		{
			foreach (IContent publishedEntity in ((PublishEventArgs<IContent>)(object)e).PublishedEntities)
			{
				try
				{
					IPublishedContent nodeById = _urlTrackerService.GetNodeById(((IEntity)publishedEntity).Id);
					if (nodeById == null)
					{
						continue;
					}
					if (((IContentBase)publishedEntity).AvailableCultures.Any())
					{
						foreach (string publishedCulture in publishedEntity.PublishedCultures)
						{
							MatchNodes(publishedEntity, nodeById, publishedCulture);
						}
					}
					else
					{
						MatchNodes(publishedEntity, nodeById);
					}
				}
				catch (Exception ex)
				{
					LoggerExtensions.Error<UrlTrackerComponent>(_logger, ex);
				}
			}
		}

		private void ContentService_Published(IContentService sender, ContentPublishedEventArgs e)
		{
			foreach (IContent publishedEntity in ((PublishEventArgs<IContent>)(object)e).PublishedEntities)
			{
				_urlTrackerService.Convert410To301ByNodeId(((IEntity)publishedEntity).Id);
			}
		}

		private void ContentService_Moving(IContentService sender, MoveEventArgs<IContent> e)
		{
			try
			{
				foreach (MoveEventInfo<IContent> item in e.MoveInfoCollection)
				{
					IContent entity = item.Entity;
					if (entity == null)
					{
						break;
					}
					IPublishedContent nodeById = _urlTrackerService.GetNodeById(((IEntity)entity).Id);
					if (nodeById == null || string.IsNullOrEmpty(nodeById.Url()) || nodeById.Parent.Id == item.NewParentId)
					{
						continue;
					}
					if (((IContentBase)entity).AvailableCultures.Any())
					{
						foreach (string availableCulture in ((IContentBase)entity).AvailableCultures)
						{
							_urlTrackerService.AddRedirect(entity, nodeById, UrlTrackerHttpCode.MovedPermanently, UrlTrackerReason.Moved, availableCulture);
						}
					}
					else
					{
						_urlTrackerService.AddRedirect(entity, nodeById, UrlTrackerHttpCode.MovedPermanently, UrlTrackerReason.Moved);
					}
				}
			}
			catch (Exception ex)
			{
				LoggerExtensions.Error<UrlTrackerComponent>(_logger, ex);
			}
		}

		private void MatchNodes(IContent newContent, IPublishedContent oldContent, string culture = "")
		{
            string text = string.IsNullOrEmpty(culture) ?
                newContent.Name :
                newContent.CultureInfos[culture].Name;
			string text2 = oldContent.Name(culture);
			string obj = ((IContentBase)newContent).GetValue("umbracoUrlName", culture, (string)null, false)?.ToString() ?? "";
			string text3 = oldContent.Value("umbracoUrlName", culture, (string)null, default(Fallback), (object)null)?.ToString() ?? "";
			if (obj != text3)
			{
				_urlTrackerService.AddRedirect(newContent, oldContent, UrlTrackerHttpCode.MovedPermanently, UrlTrackerReason.UrlOverwritten, culture);
			}
			else if (!string.IsNullOrEmpty(text2) && text != text2)
			{
				_urlTrackerService.AddRedirect(newContent, oldContent, UrlTrackerHttpCode.MovedPermanently, UrlTrackerReason.Renamed, culture);
			}
			else
			{
				if (!_urlTrackerSettings.IsSEOMetadataInstalled() || !((IContentBase)newContent).HasProperty(_urlTrackerSettings.GetSEOMetadataPropertyName()))
				{
					return;
				}
				string text4 = ((IContentBase)newContent).GetValue(_urlTrackerSettings.GetSEOMetadataPropertyName(), culture, (string)null, false)?.ToString() ?? "";
				string text5 = oldContent.Value(_urlTrackerSettings.GetSEOMetadataPropertyName(), culture, (string)null, default(Fallback), (object)null)?.ToString() ?? "";
				if (!text4.Equals(text5))
				{
					dynamic val = JObject.Parse(text4);
					string text6 = val.urlName;
					dynamic val2 = JObject.Parse(text5);
					string text7 = val2.urlName;
					if (text6 != text7)
					{
						_urlTrackerService.AddRedirect(newContent, oldContent, UrlTrackerHttpCode.MovedPermanently, UrlTrackerReason.UrlOverwrittenSEOMetadata, culture);
					}
				}
			}
		}
	}
}
