using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Douban
{
    public abstract class BaseProvider
    {
        protected const string ProviderID = "DoubanID";

        protected IHttpClient _httpClient;
        protected IJsonSerializer _jsonSerializer;
        protected ILogger _logger;

        protected Configuration.PluginConfiguration _config;

        protected BaseProvider(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
        {
            this._httpClient = httpClient;
            this._jsonSerializer = jsonSerializer;
            this._logger = logger;
            this._config = Plugin.Instance == null ?
                               new Configuration.PluginConfiguration() :
                               Plugin.Instance.Configuration;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban:GetImageResponse url: {0}", url);
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            });
        }

        internal async Task<Response.Subject> GetSubject(string sid,
                                                         CancellationToken cancellationToken)
        {
            _logger.LogInformation("Trying to get douban subject by sid: {0}", sid);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(sid))
            {
                throw new ArgumentException("sid is empty when getting subject");
            }

            String apikey = _config.ApiKey;
            var url = String.Format("http://api.douban.com/v2/movie/subject/{0}?apikey={1}",
                                    sid,
                                    apikey);
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true,
                EnableDefaultUserAgent = true,
            };

            var response = await _httpClient.GetResponse(options).ConfigureAwait(false);
            var data = await _jsonSerializer.DeserializeFromStreamAsync<Response.Subject>(response.Content);
            return data;
        }

        protected async Task<string> SearchSidByName(string name, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Trying to get sid by name: {0}", name);

            if (String.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Search name is empty.");
                return "";
            }

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

            using (var response = await _httpClient.GetResponse(options).ConfigureAwait(false))
            using (var reader = new StreamReader(response.Content))
            {
                String content = reader.ReadToEnd();
                String pattern = @"sid: (\d+)";
                Match match = Regex.Match(content, pattern);
                if (match.Success)
                {
                    var sid = match.Groups[1].Value;
                    _logger.LogInformation("The sid of {0} is {1}", name, sid);
                    return sid;
                }
            }
            return "";
        }
    }
}