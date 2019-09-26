using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
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
    public class DoubanProvider : IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
    {
        public String Name => "Douban Metadata Provider";
        public int Order => 3;

        public const string ProviderID = "DoubanID";

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        private readonly PluginConfiguration _config;


        public DoubanProvider(IHttpClient httpClient,
                              IJsonSerializer jsonSerializer,
                              ILogger logger)
        {
            this._httpClient = httpClient;
            this._jsonSerializer = jsonSerializer;
            this._logger = logger;
            this._config = Plugin.Instance == null ?
                               new Configuration.PluginConfiguration() :
                               Plugin.Instance.Configuration;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Douban:GetMetadata name: {info.Name}");

            var sid = info.GetProviderId(ProviderID);
            if (string.IsNullOrWhiteSpace(sid))
            {
                // Get subject id firstly
                sid = await GetSidByName(info.Name, cancellationToken).ConfigureAwait(false);
                info.SetProviderId(ProviderID, sid);
            }

            var result = await GetMovieItem(sid, cancellationToken).ConfigureAwait(false);
            result.QueriedById = true;
            result.HasMetadata = true;
            return result;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo info,
                CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban: search name {0}", info.Name);
            throw new NotImplementedException("Douban:GetSearchResults");
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban:GetImageResponse url: {}", url);
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            });
        }

        public async Task<string> GetSidByName(string name, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Trying to get sid by name: {0}", name);
            // TODO: Change to use the search api instead of parsing by HTML when the search api
            // is available.
            var url = String.Format("http://www.douban.com/search?cat={0}&q={1}", "1002", name);
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true,
                EnableDefaultUserAgent = true,
            };

            String sid;
            using (var response = await _httpClient.GetResponse(options).ConfigureAwait(false))
            {
                String content = new StreamReader(response.Content).ReadToEnd();
                String pattern = @"sid: (\d+)";
                Match match = Regex.Match(content, pattern);
                sid = match.Groups[1].Value;
            }
            _logger.LogInformation("The sid of {0} is {1}", name, sid);
            return sid;
        }

        public async Task<MetadataResult<Movie>> GetMovieItem(string sid, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Trying to get movie item by sid: {0}", sid);
            var result = new MetadataResult<Movie>
            {
                Item = new Movie(),
            };
            var movie = result.Item;

            String apikey = _config.ApiKey;
            var url = String.Format("http://api.douban.com/v2/movie/subject/{0}?apikey={1}", sid, apikey);
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true,
                EnableDefaultUserAgent = true,
            };

            using (var response = await _httpClient.GetResponse(options).ConfigureAwait(false))
            {
                var data = await _jsonSerializer.DeserializeFromStreamAsync<Response.Movie>(response.Content);
                movie.Name = data.Title;
                movie.OriginalTitle = data.Original_Title;
                movie.CommunityRating = data.Rating.Average;
                movie.Overview = data.Summary;
                movie.ProductionYear = int.Parse(data.Year);
                movie.PremiereDate = DateTime.Parse(data.Pubdate);
                movie.HomePageUrl = data.Alt;
                movie.ProductionLocations = data.Countries.ToArray();
                foreach (var genre in data.Genres)
                {
                    movie.AddGenre(genre);
                }

                TransPersonInfo(data.Directors, PersonType.Director).ForEach(item => result.AddPerson(item));
                TransPersonInfo(data.Casts, PersonType.Actor).ForEach(item => result.AddPerson(item));
                TransPersonInfo(data.Writers, PersonType.Writer).ForEach(item => result.AddPerson(item));
            }

            _logger.LogInformation("The name of sid {0} is {1}", sid, movie.Name);
            return result;
        }

        private List<PersonInfo> TransPersonInfo(List<Response.PersonInfo> persons, string role)
        {
            var result = new List<PersonInfo>();
            foreach (var person in persons)
            {
                var personInfo = new PersonInfo
                {
                    Name = person.Name,
                    Role = role,
                    ImageUrl = person.Avatars.Medium,
                };

                personInfo.SetProviderId(ProviderID, person.Id);
                result.Add(personInfo);
            }
            return result;
        }
    }
}