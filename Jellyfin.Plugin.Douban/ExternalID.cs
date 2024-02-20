using Jellyfin.Plugin.Douban.Providers;

using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Douban
{
    public class DoubanExternalId : IExternalId
    {
        public string ProviderName => "Douban";

        public string Key => BaseProvider.ProviderID;

        public ExternalIdMediaType? Type => null;

        public string UrlFormatString => "https://movie.douban.com/subject/{0}/";

        public bool Supports(IHasProviderIds item)
        {
            return item is Movie || item is Series;
        }
    }
}