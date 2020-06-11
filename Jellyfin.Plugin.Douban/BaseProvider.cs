using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Douban
{
    public abstract class BaseProvider
    {
        internal const string ProviderID = "DoubanID";

        protected IHttpClient _httpClient;
        protected IJsonSerializer _jsonSerializer;
        protected ILogger _logger;

        protected Configuration.PluginConfiguration _config;
        protected DoubanAccessor _doubanAccessor;

        protected BaseProvider(IHttpClient httpClient,
            IJsonSerializer jsonSerializer, ILogger logger)
        {
            this._httpClient = httpClient;
            this._jsonSerializer = jsonSerializer;
            this._logger = logger;
            this._config = Plugin.Instance == null ?
                               new Configuration.PluginConfiguration() :
                               Plugin.Instance.Configuration;

            this._doubanAccessor = new DoubanAccessor(_httpClient);
        }

        public Task<HttpResponseInfo> GetImageResponse(string url,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban:GetImageResponse url: {0}", url);
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            });
        }

        protected async Task<IEnumerable<string>> SearchSidByName(string name,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban: Trying to search sid by name: {0}",
                                   name);

            var sidList = new List<string>();

            if (String.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Search name is empty.");
                return sidList;
            }

            // TODO: Change to use the search api instead of parsing by HTML
            // when the search api is available.
            var url = String.Format("http://www.douban.com/search?cat={0}&q={1}", "1002", name);
            try
            {
                String content = await _doubanAccessor.GetResponseWithDelay(url,
                                 cancellationToken);
                String pattern = @"sid: (\d+)";
                Match match = Regex.Match(content, pattern);

                while (match.Success)
                {
                    var sid = match.Groups[1].Value;
                    _logger.LogDebug("The sid of {0} is {1}", name, sid);
                    sidList.Add(sid);

                    match = match.NextMatch();
                }
            }
            catch (HttpException e)
            {
                _logger.LogError("Could not access url: {0}, status code: {1}",
                                 url, e.StatusCode);
                throw e;
            }
            return sidList.Distinct().ToList();
        }

        protected async Task<MetadataResult<T>> GetMetaFromDouban<T>(string sid,
            string type, CancellationToken cancellationToken)
            where T : BaseItem, new()
        {
            _logger.LogInformation("Trying to get item by sid: {0}", sid);
            var result = new MetadataResult<T>();

            if (string.IsNullOrWhiteSpace(sid))
            {
                _logger.LogWarning("Can not get movie item, sid is empty");
                return result;
            }

            var data = await GetDoubanSubject(sid, cancellationToken);
            if (!String.IsNullOrEmpty(type) && data.Subtype != type)
            {
                _logger.LogInformation("Douban: Sid {1}'s type is {2}, " +
                    "but require {3}", sid, data.Subtype, type);
                return result;
            }

            result.Item = TransMediaInfo<T>(data);
            TransPersonInfo(data.Directors, PersonType.Director).ForEach(result.AddPerson);
            TransPersonInfo(data.Casts, PersonType.Actor).ForEach(result.AddPerson);
            TransPersonInfo(data.Writers, PersonType.Writer).ForEach(result.AddPerson);

            result.QueriedById = true;
            result.HasMetadata = true;

            _logger.LogInformation("Douban: The name of sid {0} is {1}",
                sid, result.Item.Name);
            return result;
        }

        internal async Task<Response.Subject> GetDoubanSubject(string sid,
                                         CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban: Trying to get douban subject by " +
                "sid: {0}", sid);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(sid))
            {
                throw new ArgumentException("sid is empty when getting subject");
            }

            String apikey = _config.ApiKey;
            var url = String.Format("http://api.douban.com/v2/movie/subject" +
                "/{0}?apikey={1}", sid, apikey);


            String content = await _doubanAccessor.GetResponse(url, cancellationToken);
            var data = _jsonSerializer.DeserializeFromString<Response.Subject>(content);

            _logger.LogInformation("Get douban subject {0} successfully: {1}",
                                   sid, data.Title);
            return data;
        }

        private T TransMediaInfo<T>(Response.Subject data)
            where T : BaseItem, new()
        {
            var media = new T
            {
                Name = data.Title,
                OriginalTitle = data.Original_Title,
                CommunityRating = data.Rating.Average,
                Overview = data.Summary.Replace("\n", "</br>"),
                ProductionYear = int.Parse(data.Year),
                HomePageUrl = data.Alt,
                ProductionLocations = data.Countries.ToArray()
            };

            if (!String.IsNullOrEmpty(data.Pubdate))
            {
                media.PremiereDate = DateTime.Parse(data.Pubdate);
            }

            data.Trailer_Urls.ForEach(item => media.AddTrailerUrl(item));
            data.Genres.ForEach(media.AddGenre);

            return media;
        }

        private List<PersonInfo> TransPersonInfo(
            List<Response.PersonInfo> persons, string personType)
        {
            var result = new List<PersonInfo>();
            foreach (var person in persons)
            {
                var personInfo = new PersonInfo
                {
                    Name = person.Name,
                    Type = personType,
                    ImageUrl = person.Avatars?.Large,
                };

                personInfo.SetProviderId(ProviderID, person.Id);
                result.Add(personInfo);
            }
            return result;
        }
    }
}