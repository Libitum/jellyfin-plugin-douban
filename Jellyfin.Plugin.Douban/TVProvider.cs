using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Douban
{
    public class TVProvider : BaseProvider, IHasOrder,
        IRemoteMetadataProvider<Series, SeriesInfo>,
        IRemoteMetadataProvider<Season, SeasonInfo>,
        IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        public string Name => "Douban TV Provider";
        public int Order => 3;

        public TVProvider(IHttpClient httpClient,
                          IJsonSerializer jsonSerializer,
                          ILogger<TVProvider> logger) : base(httpClient, jsonSerializer, logger)
        {
            // empty
        }

        #region series
        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info,
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
                return new MetadataResult<Series>();
            }

            var result = await GetMetaFromFrodo<Series>(sid, "tv",
                cancellationToken).ConfigureAwait(false);
            if (result.HasMetadata)
            {
                _logger.LogInformation($"[DOUBAN FRODO INFO] Get the metadata of \"{info.Name}\" successfully!");
            }

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            SeriesInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[DOUBAN FRODO INFO] Searching \"{info.Name}\"");

            var results = new List<RemoteSearchResult>();

            var searchResults = new List<Response.SearchTarget>();

            string sid = info.GetProviderId(ProviderID);
            if (!string.IsNullOrEmpty(sid))
            {
                searchResults.Add(FrodoUtils.MapSubjectToSearchTarget(await GetFrodoSubject(sid, "tv", cancellationToken)));
            }
            else
            {
                searchResults = await SearchFrodoByName(info.Name, "tv", cancellationToken).
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
        #endregion series

        #region season
        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[DOUBAN FRODO INFO] Getting metadata for \"{info.Name}\"");
            var result = new MetadataResult<Season>();

            info.SeriesProviderIds.TryGetValue(ProviderID, out string seriesId);
            var sid = info.GetProviderId(ProviderID);
            if (string.IsNullOrEmpty(sid))
            {
                var searchResults = await SearchFrodoByName(info.Name, "tv",
                   cancellationToken).ConfigureAwait(false);
                sid = searchResults.FirstOrDefault()?.Id;
            }

            if (string.IsNullOrWhiteSpace(sid))
            {
                _logger.LogError($"[DOUBAN FRODO ERROR] No sid found for \"{info.Name}\"");
                return new MetadataResult<Season>();
            }

            var subject = await GetFrodoSubject(sid, "tv", cancellationToken).
                ConfigureAwait(false);

            string pattern_name = @".* (?i)Season(?-i) (\d+)$";
            Match match = Regex.Match(subject.Original_Title, pattern_name);
            if (match.Success)
            {
                result.Item = new Season
                {
                    IndexNumber = int.Parse(match.Groups[1].Value),
                    ProductionYear = int.Parse(subject.Year)
                };
                result.HasMetadata = true;
            }
            return result;

        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            SeasonInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban:Search for season {0}", info.Name);
            // It's needless for season to do search
            return Task.FromResult<IEnumerable<RemoteSearchResult>>(
                new List<RemoteSearchResult>());
        }
        #endregion season

        #region episode
        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info,
                                              CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Douban:GetMetadata for episode {info.Name}");
            var result = new MetadataResult<Episode>();

            if (info.IsMissingEpisode)
            {
                _logger.LogInformation("Do not support MissingEpisode");
                return result;
            }

            var sid = info.GetProviderId(ProviderID);
            if (string.IsNullOrEmpty(sid))
            {
                var searchResults = await SearchFrodoByName(info.Name, "tv",
                   cancellationToken).ConfigureAwait(false);
                sid = searchResults.FirstOrDefault()?.Id;
            }

            if (!info.IndexNumber.HasValue)
            {
                _logger.LogInformation("No episode num found, please check " +
                    "the format of file name");
                return result;
            }
            // Start to get information from douban
            result.Item = new Episode
            {
                Name = info.Name,
                IndexNumber = info.IndexNumber,
                ParentIndexNumber = info.ParentIndexNumber
            };
            result.Item.SetProviderId(ProviderID, sid);

            var url = string.Format("https://movie.douban.com/subject/{0}" +
                "/episode/{1}/", sid, info.IndexNumber);
            string content = await _doubanAccessor.GetResponseWithDelay(url, cancellationToken);
            string pattern_name = "data-name=\\\"(.*?)\\\"";
            Match match = Regex.Match(content, pattern_name);
            if (match.Success)
            {
                var name = HttpUtility.HtmlDecode(match.Groups[1].Value);
                _logger.LogDebug("The name is {0}", name);
                result.Item.Name = name;
            }

            string pattern_desc = "data-desc=\\\"(.*?)\\\"";
            match = Regex.Match(content, pattern_desc);
            if (match.Success)
            {
                var desc = HttpUtility.HtmlDecode(match.Groups[1].Value);
                _logger.LogDebug("The desc is {0}", desc);
                result.Item.Overview = desc;
            }
            result.HasMetadata = true;

            return result;
        }


        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            EpisodeInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban:Search for episode {0}", info.Name);
            // It's needless for season to do search
            return Task.FromResult<IEnumerable<RemoteSearchResult>>(
                new List<RemoteSearchResult>());
        }
        #endregion episode
    }
}
