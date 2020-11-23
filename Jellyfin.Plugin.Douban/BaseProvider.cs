using System;
using System.Collections.Generic;
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

            this._doubanAccessor = new DoubanAccessor(_httpClient, _logger,
                                                      _config.MinRequestInternalMs);
        }

        public Task<HttpResponseInfo> GetImageResponse(string url,
           CancellationToken cancellationToken)
        {
            _logger.LogInformation("[DOUBAN INFO] GetImageResponse url: {0}", url);
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            });
        }

        public async Task<List<Response.SearchTarget>> SearchFrodoByName(string name, string type,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[DOUBAN FRODO INFO] Searching for sid of {type} named \"{name}\"");

            var searchResults = new List<Response.SearchTarget>();

            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning($"[DOUBAN FRODO WARN] Search name is empty.");
                return searchResults;
            }

            name = string.Join(" ", name.Split("."));

            SearchCache searchCache = SearchCache.Instance;
            string searchId = $"{name}-{type}";
            if (searchCache.Has(searchId))
            {
                _logger.LogInformation($"[DOUBAN FRODO INFO] Found search cache.");
                return searchCache.searchResult;
            }

            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            queryParams.Add("q", name);
            queryParams.Add("count", $"{FrodoUtils.MaxSearchCount}");

            try
            {
                var response = await _doubanAccessor.RequestFrodo(FrodoUtils.SearchApi, queryParams,
                    cancellationToken);
                Response.SearchResult result = _jsonSerializer.DeserializeFromString<Response.SearchResult>(response);
                if (result.Total > 0)
                {
                    foreach (Response.SearchSubject subject in result.Items)
                    {
                        if (subject.Target_Type == type)
                        {
                            searchResults.Add(subject.Target);
                        }
                    }
                    if (searchResults.Count == 0)
                    {
                        _logger.LogWarning($"[DOUBAN FRODO WARN] Seems like \"{name}\" genre is not {type}.");
                    }
                }
                else
                {
                    _logger.LogError($"[DOUBAN FRODO ERR] No results found for \"{name}\".");
                }
            }
            catch (HttpException e)
            {
                _logger.LogError($"[DOUBAN FRODO ERR] Search \"{name}\" error, got {e.StatusCode}.");
                throw e;
            }

            searchCache.SetSearchCache(searchId, searchResults);

            return searchResults;
        }

        protected async Task<MetadataResult<T>> GetMetaFromFrodo<T>(string sid,
            string type, CancellationToken cancellationToken)
        where T : BaseItem, new()
        {
            var result = new MetadataResult<T>();

            var subject = await GetFrodoSubject(sid, type, cancellationToken);

            result.Item = FrodoUtils.MapSubjectToItem<T>(subject);
            result.Item.SetProviderId(ProviderID, sid);
            FrodoUtils.MapCrewToPersons(subject.Directors, PersonType.Director).ForEach(result.AddPerson);
            FrodoUtils.MapCrewToPersons(subject.Actors, PersonType.Actor).ForEach(result.AddPerson);

            result.QueriedById = true;
            result.HasMetadata = true;


            return result;
        }

        internal async Task<Response.Subject> GetFrodoSubject(string sid, string type,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[DOUBAN FRODO INFO] Getting the douban subject with sid \"{sid}\"");

            SubjectCache subjectCache = SubjectCache.Instance;

            if (subjectCache.Has(sid))
            {
                _logger.LogInformation($"[DOUBAN FRODO INFO] Found cache.");
                return subjectCache.subject;
            }

            String response = await _doubanAccessor.RequestFrodo($"{FrodoUtils.ItemApi}/{type}/{sid}", new Dictionary<string, string>(), cancellationToken);
            Response.Subject subject = _jsonSerializer.DeserializeFromString<Response.Subject>(response);
            subjectCache.subject = subject;
            return subject;
        }
    }
}