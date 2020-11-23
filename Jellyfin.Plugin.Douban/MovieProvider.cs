using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Douban
{
    public class MovieProvider : BaseProvider, IHasOrder,
        IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public string Name => "Douban Movie Provider";
        public int Order => 3;

        public MovieProvider(IHttpClient httpClient,
            IJsonSerializer jsonSerializer,
            ILogger<MovieProvider> logger) : base(httpClient, jsonSerializer, logger)
        {
            // Empty
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[DOUBAN FRODO INFO] Getting metadata for \"{info.Name}\"");

            var sid = info.GetProviderId(ProviderID);
            if (string.IsNullOrWhiteSpace(sid))
            {
                var searchResults = await SearchFrodoByName(info.Name, "movie",
                    cancellationToken).ConfigureAwait(false);
                sid = searchResults.FirstOrDefault()?.Id;
            }

            if (string.IsNullOrWhiteSpace(sid))
            {
                _logger.LogError($"[DOUBAN FRODO ERROR] No sid found for \"{info.Name}\"");
                return new MetadataResult<Movie>();
            }

            var result = await GetMetaFromFrodo<Movie>(sid, "movie",
                cancellationToken).ConfigureAwait(false);
            if (result.HasMetadata)
            {
                _logger.LogInformation($"[DOUBAN FRODO INFO] Get the metadata of \"{info.Name}\" successfully!");
                info.SetProviderId(ProviderID, sid);
            }

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            MovieInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[DOUBAN FRODO INFO] Searching \"{info.Name}\"");

            var results = new List<RemoteSearchResult>();

            var searchResults = new List<Response.SearchTarget>();

            string sid = info.GetProviderId(ProviderID);
            if (!string.IsNullOrEmpty(sid))
            {
                searchResults.Add(FrodoUtils.MapSubjectToSearchTarget(await GetFrodoSubject(sid, "movie", cancellationToken)));
            }
            else
            {
                searchResults = await SearchFrodoByName(info.Name, "movie", cancellationToken).
                ConfigureAwait(false);
            }

            foreach (Response.SearchTarget searchTarget in searchResults)
            {
                var searchResult = new RemoteSearchResult()
                {
                    Name = searchTarget?.Title,
                    ImageUrl = searchTarget?.Cover_Url,
                    ProductionYear = int.Parse(searchTarget?.Year)
                };
                searchResult.SetProviderId(ProviderID, searchTarget.Id);
                results.Add(searchResult);
            }

            return results;
        }

    }
}