using InfoCaster.Umbraco.UrlTracker.Helpers;
using InfoCaster.Umbraco.UrlTracker.Repositories;
using InfoCaster.Umbraco.UrlTracker.Services;
using InfoCaster.Umbraco.UrlTracker.Settings;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace InfoCaster.Umbraco.UrlTracker
{
	[RuntimeLevel(/*Could not decode attribute arguments.*/)]
	public class UrlTrackerComposer : IUserComposer, IComposer, IDiscoverable
	{
		public void Compose(Composition composition)
		{
			((OrderedCollectionBuilderBase<ComponentCollectionBuilder, ComponentCollection, IComponent>)(object)CompositionExtensions.Components(composition)).Append<UrlTrackerComponent>();
			RegisterExtensions.Register<IUrlTrackerHelper, UrlTrackerHelper>((IRegister)(object)composition, (Lifetime)0);
			RegisterExtensions.Register<IUrlTrackerLoggingHelper, UrlTrackerLoggingHelper>((IRegister)(object)composition, (Lifetime)0);
			RegisterExtensions.Register<IUrlTrackerRepository, UrlTrackerRepository>((IRegister)(object)composition, (Lifetime)0);
			RegisterExtensions.Register<IUrlTrackerCacheService, UrlTrackerCacheService>((IRegister)(object)composition, (Lifetime)0);
			RegisterExtensions.Register<IUrlTrackerService, UrlTrackerService>((IRegister)(object)composition, (Lifetime)0);
			RegisterExtensions.Register<IUrlTrackerSettings, UrlTrackerSettings>((IRegister)(object)composition, (Lifetime)3);
		}
	}
}
