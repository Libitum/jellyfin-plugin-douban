using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Douban
{
    public class ImageProvider : BaseProvider, IRemoteImageProvider, IHasOrder
    {
        public string Name => "Douban Image Provider";
        public int Order => 3;

        public ImageProvider(IHttpClient httpClient,
                                   IJsonSerializer jsonSerializer,
                                   ILogger logger) : base(httpClient, jsonSerializer, logger)
        {
            // empty
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();
            var sid = item.GetProviderId(ProviderID);
            if (string.IsNullOrWhiteSpace(sid))
            {
                // Not by douban
                _logger.LogWarning("GetImages failed because that the sid is empty: {Name}", item.Name);
                return list;
            }
            var movie =  await GetSubject(sid, cancellationToken);
            list.Add(new RemoteImageInfo{
                ProviderName = Name,
                Url = movie.Images.Large,
            });
            return list;
        }

        public bool Supports(BaseItem item)
        {
            return item is Movie;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }
    }
}