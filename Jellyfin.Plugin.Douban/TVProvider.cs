using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;

using Jellyfin.Plugin.Douban.Configuration;

namespace Jellyfin.Plugin.Douban
{
    public class TVProvider : BaseProvider, IHasOrder,
        IRemoteMetadataProvider<Series, SeriesInfo>,
        IRemoteMetadataProvider<Season, SeasonInfo>,
        IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        public String Name => "Douban TV Provider";
        public int Order => 3;

        public TVProvider(IHttpClient httpClient,
                          IJsonSerializer jsonSerializer,
                          ILogger logger) : base(httpClient, jsonSerializer, logger)
        {
            // empty
        }

        #region series
        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Douban:GetMetadata name: {info.Name}");


            // Only handle it when launguage is "zh"
            if (info.MetadataLanguage != "zh")
            {
                _logger.LogInformation("DoubanProvider: the required " +
                    "launguage is not zh, so just bypass DoubanProvider");
                return new MetadataResult<Series>();
            }

            var sid = info.GetProviderId(ProviderID);
            _logger.LogInformation($"sid: {sid}");
            if (string.IsNullOrWhiteSpace(sid))
            {
                // Get subject id firstly
                var sidList = await SearchSidByName(info.Name,
                    cancellationToken).ConfigureAwait(false);
                foreach (var s in sidList)
                {
                    _logger.LogDebug($"sidList: {s}");
                }
                sid = sidList.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(sid))
            {
                // Not found, just return
                return new MetadataResult<Series>();
            }

            var result = await GetMetaFromDouban<Series>(sid, "tv",
                cancellationToken).ConfigureAwait(false);
            if (result.HasMetadata)
            {
                info.SetProviderId(ProviderID, sid);
            }

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
            SeriesInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban: search name {0}", info.Name);

            var results = new List<RemoteSearchResult>();

            // Only handle it when launguage is "zh"
            if (info.MetadataLanguage != "zh")
            {
                _logger.LogInformation("DoubanProvider: the required " +
                    "launguage is not zh, so just bypass DoubanProvider");
                return results;
            }

            var sidList = await SearchSidByName(info.Name, cancellationToken).
                ConfigureAwait(false);
            foreach (String sid in sidList)
            {
                var subject = await GetDoubanSubject(sid, cancellationToken).
                    ConfigureAwait(false);
                if (subject.Subtype != "tv")
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
        #endregion series

        #region season
        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Douban:GetMetadata for {info.Name}");
            var result = new MetadataResult<Season>();

            info.SeriesProviderIds.TryGetValue(ProviderID, out string sid);
            if (string.IsNullOrEmpty(sid))
            {
                _logger.LogInformation("No douban sid found, just skip");
                return result;
            }

            if (info.IndexNumber.HasValue && info.IndexNumber.Value > 0)
            {
                // We can not give more information from Douban right now.
                return result;
            }

            var subject = await GetDoubanSubject(sid, cancellationToken).
                ConfigureAwait(false);
            if (subject.Current_Season.HasValue)
            {
                result.Item = new Season
                {
                    IndexNumber = subject.Current_Season.Value,
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

            info.SeriesProviderIds.TryGetValue(ProviderID, out string sid);
            if (string.IsNullOrEmpty(sid))
            {
                _logger.LogInformation("No douban sid found, just skip");
                return result;
            }

            if (!info.IndexNumber.HasValue)
            {
                _logger.LogInformation("No episode num found, please check " +
                    "the format of file name");
                return result;
            }
            // Start to get information from douban
            var url = String.Format("https://movie.douban.com/subject/{0}" +
                "/episode/{1}/", sid, info.IndexNumber);
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true,
                EnableDefaultUserAgent = true,
            };

            result.Item = new Episode
            {
                Name = info.Name,
                IndexNumber = info.IndexNumber,
                ParentIndexNumber = info.ParentIndexNumber
            };
            using (var response = await _httpClient.GetResponse(options).
                ConfigureAwait(false))
            using (var reader = new StreamReader(response.Content))
            {
                String content = reader.ReadToEnd();
                String pattern_name = "data-name=\\\"(.*?)\\\"";
                Match match = Regex.Match(content, pattern_name);
                if (match.Success)
                {
                    var name = match.Groups[1].Value;
                    _logger.LogDebug("The name is {0}", name);
                    result.Item.Name = name;
                }

                String pattern_desc = "data-desc=\\\"(.*?)\\\"";
                match = Regex.Match(content, pattern_desc);
                if (match.Success)
                {
                    var desc = match.Groups[1].Value;
                    _logger.LogDebug("The desc is {0}", desc);
                    result.Item.Overview = desc;
                }
                result.HasMetadata = true;
            }

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
