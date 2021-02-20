using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Douban
{
    /// <summary>
    /// Frodo is the secondary domain of API used by Douban APP.
    /// </summary>
    public sealed class FrodoAndroidClient : IDoubanClient
    {

        private const string BaseDoubanUrl = "https://frodo.douban.com";

        /// API key to use when performing an API call.
        private const string ApiKey = "0dad551ec0f84ed02907ff5c42e8ec70";

        /// Secret key for HMACSHA1 to generate signature.
        private const string SecretKey = "bf7dddc7c9cfe6f7";

        private static readonly string[] UserAgents = {
            "api-client/1 com.douban.frodo/6.42.2(194) Android/22 product/shamu vendor/OPPO model/OPPO R11 Plus rom/android network/wifi platform/mobile nd/1",
            "api-client/1 com.douban.frodo/6.42.2(194) Android/23 product/meizu_MX6 vendor/Meizu model/MX6 rom/android network/wifi platform/mobile",
            "api-client/1 com.douban.frodo/6.32.0(180) Android/23 product/OnePlus3 vendor/One model/One rom/android network/wifi",
            "api-client/1 com.douban.frodo/6.32.0(180) Android/25 product/Google vendor/LGE model/Nexus 5 rom/android network/wifi platform/mobile nd/1",
            "api-client/1 com.douban.frodo/7.0.1(204) Android/28 product/hammerhead vendor/Xiaomi model/MI 10 rom/android network/wifi platform/mobile nd/1",
            "api-client/1 com.douban.frodo/6.32.0(180) Android/26 product/marlin vendor/Google model/Pixel XL rom/android network/wifi platform/mobile nd/1",
            "api-client/1 com.douban.frodo/7.0.1(204) Android/29 product/nitrogen vendor/Xiaomi model/MI MAX 3 rom/miui6 network/wifi  platform/mobile nd/1",
            "api-client/1 com.douban.frodo/6.32.0(180) Android/22 product/R11 vendor/OPPO model/OPPO R11 rom/android network/wifi  platform/mobile nd/1",
        };

        private static readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        private static readonly LRUCache _cache = new LRUCache();

        private readonly Random _random = new Random();

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        private string _userAgent;
        private int _requestCount = 0;

        public FrodoAndroidClient(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer,
            ILogger logger)
        {
            this._httpClientFactory = httpClientFactory;
            this._jsonSerializer = jsonSerializer;
            this._logger = logger;

            this._userAgent = UserAgents[_random.Next(UserAgents.Length)];
        }

        /// <summary>
        /// Gets one movie or tv item by doubanID.
        /// </summary>
        /// <param name="doubanID">The subject ID in Douban.</param>
        /// <param name="type">Subject type.</param>
        /// <param name="cancellationToken">Used to cancel the request.</param>
        /// <returns>The subject of one item.</returns>
        public async Task<Response.Subject> GetSubject(string doubanID, MediaType type, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Start to GetSubject by Id: {doubanID}");

            string path = $"/api/v2/{type:G}/{doubanID}";
            // Try to use cache firstly.
            if (_cache.TryGet<Response.Subject>(path, out Response.Subject subject))
            {
                _logger.LogInformation($"Get subject {doubanID} from cache");
                return subject;
            }

            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            var contentStream = await GetResponse(path, queryParams, cancellationToken);
            subject = await _jsonSerializer.DeserializeFromStreamAsync<Response.Subject>(contentStream);
            // Add it into cache
            _cache.Add(path, subject);

            _logger.LogTrace($"Finish doing GetSubject by Id: {doubanID}");
            return subject;
        }

        /// <summary>
        /// Search in Douban by a search query.
        /// </summary>
        /// <param name="name">The content of search query.</param>
        /// <param name="cancellationToken">Used to cancel the request.</param>
        /// <returns>The Search Result.</returns>
        public async Task<Response.SearchResult> Search(string name, CancellationToken cancellationToken)
        {
            return await Search(name, 5, cancellationToken);
        }

        public async Task<Response.SearchResult> Search(string name, int count, CancellationToken cancellationToken)
        {
            await _locker.WaitAsync(cancellationToken);

            // Change UserAgent for every search section.
            _userAgent = UserAgents[_random.Next(UserAgents.Length)];
            ResetCounter();

            try
            {
                _logger.LogInformation($"Start to Search by name: {name}, count: {count}");
                
                await Task.Delay(_random.Next(4000, 10000), cancellationToken);

                const string path = "/api/v2/search/movie";
                Dictionary<string, string> queryParams = new Dictionary<string, string>
                {
                    { "q", name },
                    { "count", count.ToString() }
                };
                var contentStream = await GetResponse(path, queryParams, cancellationToken);
                Response.SearchResult result = await _jsonSerializer.DeserializeFromStreamAsync<Response.SearchResult>(contentStream);

                _logger.LogTrace($"Finish doing Search by name: {name}, count: {count}");

                return result;
            }
            finally
            {
                _locker.Release();
            }
        }

        /// <summary>
        /// Generates signature for douban api
        /// </summary>
        /// <param name="path">Douban api path, e.g. /api/v2/search/movie</param>
        /// <param name="ts">Timestamp.</param>
        /// <returns>Douban signature</returns>
        private static string Sign(string path, string ts)
        {
            string[] message =
            {
                "GET",
                path.Replace("/", "%2F"),
                ts
            };
            string signMessage = String.Join('&', message);

            using var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(SecretKey));
            byte[] sign = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(signMessage));

            return Convert.ToBase64String(sign);
        }

        /// <summary>
        /// Sends request to Douban Frodo and get the response.
        /// It generates the signature for douban api internally.
        /// </summary>
        /// <param name="path">Douban api path, e.g. /api/v2/search/movie</param>
        /// <param name="queryParams">Parameters for the request.</param>
        /// <param name="cancellationToken">Used to cancel the request.</param>
        /// <returns>The HTTP content with the type of stream.</returns>
        private async Task<Stream> GetResponse(string path, Dictionary<string, string> queryParams,
                CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Start to request path: {path}");

            cancellationToken.ThrowIfCancellationRequested();

            // Sign for the parameters.
            string ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            queryParams.Add("_ts", ts);
            queryParams.Add("_sig", Sign(path, ts));
            queryParams.Add("apikey", ApiKey);

            // Generate the URL.
            string queryStr = string.Join('&', queryParams.Select(item => $"{item.Key}={HttpUtility.UrlEncode(item.Value)}"));
            string url = $"{BaseDoubanUrl}{path}?{queryStr}";
            _logger.LogInformation($"Frodo request URL: {url}");

            // Send request to Frodo API and get response.
            using HttpResponseMessage response = await GetAsync(url, cancellationToken);
            using Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);

            _logger.LogTrace($"Finish doing request path: {path}");
            return content;
        }


        /// <summary>
        /// Simply gets the response by HTTP without any other options.
        /// </summary>
        /// <param name="url">Request URL.</param>
        /// <param name="cancellationToken">Used to cancel the request.</param>
        /// <returns>Simple Http Response.</returns>
        public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            
            cancellationToken.ThrowIfCancellationRequested();

            // await Task.Delay(6000, cancellationToken);
            CheckCountAndSleep();

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _userAgent);

            HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response; 
        }

        private void ResetCounter()
        {
            _requestCount = 0;
        }

        private void CheckCountAndSleep()
        {
            if (_requestCount > 5)
            {
                Task.Delay(_random.Next(3000, 7000));
                _requestCount = 0;
            }
            _requestCount++;
        }
    }
}
