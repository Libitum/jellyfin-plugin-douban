using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Douban.Response;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;

using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Douban
{
    public abstract class BaseProvider
    {
        /// <summary>
        /// Used to store douban Id in Jellyfin system. 
        /// </summary>
        public const string ProviderID = "DoubanID";

        protected readonly ILogger _logger;

        protected readonly Configuration.PluginConfiguration _config;

        // All requests 
        protected readonly IDoubanClient _doubanClient;

        protected BaseProvider(IHttpClientFactory httpClientFactory,
            IJsonSerializer jsonSerializer, ILogger logger)
        {
            this._logger = logger;
            this._config = Plugin.Instance == null ?
                               new Configuration.PluginConfiguration() :
                               Plugin.Instance.Configuration;

            this._doubanClient = new FrodoAndroidClient(httpClientFactory, jsonSerializer, logger);
        }

        public Task<HttpResponseMessage> GetImageResponse(string url,
           CancellationToken cancellationToken)
        {
            _logger.LogInformation("[DOUBAN] GetImageResponse url: {0}", url);
            return _doubanClient.GetAsync(url, cancellationToken);
        }

        public async Task<List<Response.SearchTarget>> Search<T>(string name,
            CancellationToken cancellationToken)
        {
            MediaType type = typeof(T) == typeof(Movie) ? MediaType.movie : MediaType.tv;

            _logger.LogInformation($"[DOUBAN] Searching for sid of {type} named \"{name}\"");

            var searchResults = new List<Response.SearchTarget>();

            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning($"[DOUBAN] Search name is empty.");
                return searchResults;
            }

            name = name.Replace('.', ' ');

            try
            {
                var response = await _doubanClient.Search(name, cancellationToken);
                if (response.Items.Count > 0)
                {
                    searchResults = response.Items.Where(item => item.Target_Type == type.ToString())
                        .Select(item => item.Target).ToList();

                    if (searchResults.Count == 0)
                    {
                        _logger.LogWarning($"[DOUBAN] Seems like \"{name}\" genre is not {type}.");
                    }
                }
                else
                {
                    _logger.LogWarning($"[DOUBAN] No results found for \"{name}\".");
                }
            }
            catch (HttpRequestException e)
            {
                _logger.LogError($"[DOUBAN] Search \"{name}\" error, got {e.StatusCode}.");
                throw;
            }

            _logger.LogInformation($"[DOUBAN] Finish searching {name}, count: {searchResults.Count}");
            return searchResults;
        }

        protected async Task<Response.Subject> GetSubject<T>(string sid,
            CancellationToken cancellationToken) where T : BaseItem
        {
            MediaType type = typeof(T) == typeof(Movie) ? MediaType.movie : MediaType.tv;
            return await _doubanClient.GetSubject(sid, type, cancellationToken);
        }

        protected async Task<MetadataResult<T>> GetMetadata<T>(string sid, CancellationToken cancellationToken)
        where T : BaseItem, new()
        {
            var result = new MetadataResult<T>();

            MediaType type = typeof(T) == typeof(Movie) ? MediaType.movie : MediaType.tv;
            var subject = await _doubanClient.GetSubject(sid, type, cancellationToken);

            result.Item = TransMediaInfo<T>(subject);
            result.Item.SetProviderId(ProviderID, sid);
            TransPersonInfo(subject.Directors, PersonType.Director).ForEach(result.AddPerson);
            TransPersonInfo(subject.Actors, PersonType.Actor).ForEach(result.AddPerson);

            result.QueriedById = true;
            result.HasMetadata = true;

            return result;
        }

        private static T TransMediaInfo<T>(Subject data) where T : BaseItem, new()
        {
            var item = new T
            {
                Name = data.Title ?? data.Original_Title,
                OriginalTitle = data.Original_Title,
                CommunityRating = data.Rating?.Value,
                Overview = data.Intro,
                ProductionYear = int.Parse(data.Year),
                HomePageUrl = data.Url,
                ProductionLocations = data.Countries?.ToArray()
            };

            if (data.Pubdate?.Count > 0 && !String.IsNullOrEmpty(data.Pubdate[0]))
            {
                string pubdate = data.Pubdate[0].Split('(', 2)[0];
                if (DateTime.TryParse(pubdate, out DateTime dateValue))
                {
                    item.PremiereDate = dateValue;
                }
            }

            if (data.Trailer != null)
            {
                item.AddTrailerUrl(data.Trailer.Video_Url);
            }

            data.Genres.ForEach(item.AddGenre);

            return item;
        }

        private static List<PersonInfo> TransPersonInfo(
            List<Crew> crewList, string personType)
        {
            var result = new List<PersonInfo>();
            foreach (var crew in crewList)
            {
                var personInfo = new PersonInfo
                {
                    Name = crew.Name,
                    Type = personType,
                    ImageUrl = crew.Avatar?.Large ?? "",
                    Role = crew.Roles.Count > 0 ? crew.Roles[0] : ""
                };

                personInfo.SetProviderId(ProviderID, crew.Id);
                result.Add(personInfo);
            }
            return result;
        }
    }
}