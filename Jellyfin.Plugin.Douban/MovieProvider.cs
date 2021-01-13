using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

        public MovieProvider(IHttpClientFactory httpClientFactory,
            IJsonSerializer jsonSerializer,
            ILogger<MovieProvider> logger) : base(httpClientFactory, jsonSerializer, logger)
        {
            // Empty
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[DOUBAN] Getting metadata for \"{info.Name}\"");

            string sid = info.GetProviderId(ProviderID);
            if (string.IsNullOrWhiteSpace(sid))
            {
                var searchResults = await Search<Movie>(info.Name, cancellationToken);
                sid = searchResults.FirstOrDefault()?.Id;
            }

            if (string.IsNullOrWhiteSpace(sid))
            {
                _logger.LogWarning($"[DOUBAN] No sid found for \"{info.Name}\"");
                return new MetadataResult<Movie>();
            }

            var result = await GetMetadata<Movie>(sid, cancellationToken);
            if (result.HasMetadata)
            {
                _logger.LogInformation($"[DOUBAN] Get the metadata of \"{info.Name}\" successfully!");
                info.SetProviderId(ProviderID, sid);
            }

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            MovieInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[DOUBAN] GetSearchResults \"{info.Name}\"");

            var results = new List<RemoteSearchResult>();

            var searchResults = new List<Response.SearchTarget>();

            string sid = info.GetProviderId(ProviderID);
            if (!string.IsNullOrEmpty(sid))
            {
                var subject = await GetSubject<Movie>(sid, cancellationToken);
                searchResults.Add(new Response.SearchTarget
                {
                    Id = subject?.Id,
                    Cover_Url = subject?.Pic?.Normal,
                    Year = subject?.Year,
                    Title = subject?.Title
                });
            }
            else
            {
                searchResults = await Search<Movie>(info.Name, cancellationToken);
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