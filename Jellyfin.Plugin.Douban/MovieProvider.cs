using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;

using Jellyfin.Plugin.Douban.Configuration;

namespace Jellyfin.Plugin.Douban
{
    public class MovieProvider : BaseProvider, IHasOrder,
        IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public String Name => "Douban Movie Provider";
        public int Order => 3;

        public MovieProvider(IHttpClient httpClient,
                              IJsonSerializer jsonSerializer,
                              ILogger logger) : base(httpClient, jsonSerializer, logger)
        {
            // Empty
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Douban:GetMetadata movie name: {info.Name}");

            // Only handle it when language is "zh"
            if (info.MetadataLanguage != "zh")
            {
                _logger.LogInformation("DoubanProvider: the required " +
                    "language is not zh, so just bypass DoubanProvider");
                return new MetadataResult<Movie>();
            }

            var sid = info.GetProviderId(ProviderID);
            if (string.IsNullOrWhiteSpace(sid))
            {
                // Get subject id firstly
                var sidList = await SearchSidByName(info.Name,
                    cancellationToken).ConfigureAwait(false);
                sid = sidList.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(sid))
            {
                // Not found, just return
                return new MetadataResult<Movie>();
            }

            var result = await GetMetaFromDouban<Movie>(sid, "movie",
                cancellationToken).ConfigureAwait(false);
            if (result.HasMetadata)
            {
                info.SetProviderId(ProviderID, sid);
            }

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            MovieInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban: search name {0}", info.Name);

            var results = new List<RemoteSearchResult>();

            // Only handle it when language is "zh"
            if (info.MetadataLanguage != "zh")
            {
                _logger.LogInformation("DoubanProvider: the required " +
                    "language is not zh, so just bypass DoubanProvider");
                return results;
            }

            var sidList = await SearchSidByName(info.Name, cancellationToken).
                ConfigureAwait(false);
            foreach (String sid in sidList)
            {
                var subject = await GetDoubanSubject(sid, cancellationToken).
                    ConfigureAwait(false);
                if (subject.Subtype != "movie")
                {
                    continue;
                }

                var searchResult = new RemoteSearchResult()
                {
                    Name = subject.Title,
                    ImageUrl = subject.Images.Large,
                    Overview = subject.Summary,
                    ProductionYear = int.Parse(subject.Year),
                };
                searchResult.SetProviderId(ProviderID, sid);
                results.Add(searchResult);
            }

            return results;
        }

    }
}
