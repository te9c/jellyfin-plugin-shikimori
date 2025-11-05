using System.Xml.Serialization;
using MediaBrowser.Common.Plugins;
using Jellyfin.Plugin.Shikimori.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Shikimori
{
    public class ShikimoriPlugin : BasePlugin<PluginConfiguration>, IHasWebPages, IPluginServiceRegistrator
    {
        public IHttpClientFactory HttpClientFactory { get; init; }
        public ShikimoriPlugin(
                IApplicationPaths applicationPaths,
                IXmlSerializer xmlSerialize,
                IHttpClientFactory httpClientFactory)
            : base(applicationPaths, xmlSerialize)
        {
            Instance = this;
            HttpClientFactory = httpClientFactory;
        }

        public const string ProviderName = "Shikimori";
        public const string ProviderId = "Shikimori";
        public const string ShikimoriBaseUrl = "https://shikimori.one";

        public override string Name => "Shikimori";
        public override Guid Id => Guid.Parse("7edb2a28-5b8a-4fe8-ae11-d941315eb862");

        public static ShikimoriPlugin? Instance { get; private set; }

        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<ShikimoriClientManager>();
            serviceCollection.AddSingleton<ProviderIdResolver>();
            serviceCollection.AddHostedService<ShikimoriStartupTask>();
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.config.html"
                }
            };
        }
    }
}
