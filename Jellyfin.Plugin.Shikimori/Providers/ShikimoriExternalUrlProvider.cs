using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Shikimori.Providers
{
    public class ShikimoriExternalUrlProvider : IExternalUrlProvider
    {
        public string Name => ShikimoriPlugin.ProviderName;

        public IEnumerable<string> GetExternalUrls(BaseItem item)
        {
            if (item.TryGetProviderId(ShikimoriPlugin.ProviderName, out var externalId))
            {
                if (item is Series or Movie)
                {
                    yield return ShikimoriPlugin.ShikimoriBaseUrl + $"/animes/{externalId}";
                }
            }
        }
    }
}