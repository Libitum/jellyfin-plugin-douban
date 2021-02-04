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
using MediaBrowser.Common.Net;
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

        private const string UserAgent = "api-client/1 com.douban.frodo/6.42.2(194) Android/22 "
            + "product/shamu vendor/OPPO model/OPPO R11 Plus"
            + "rom/android  network/wifi  platform/mobile nd/1";

        private static readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        private readonly Random _random = new Random();

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        public FrodoAndroidClient(IHttpClient httpClient, IJsonSerializer jsonSerializer,
            ILogger logger)
        {
            this._httpClient = httpClient;
            this._jsonSerializer = jsonSerializer;
            this._logger = logger;
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
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            var contentStream = await GetResponse(path, queryParams, cancellationToken);
            Response.Subject subject = await _jsonSerializer.DeserializeFromStreamAsync<Response.Subject>(contentStream);

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
            await _locker.WaitAsync();
            try
            {
                _logger.LogInformation($"Start to Search by name: {name}, count: {count}");

                const string path = "/api/v2/search/movie";
                Dictionary<string, string> queryParams = new Dictionary<string, string>
                {
                    { "q", name },
                    { "count", count.ToString() }
                };
                var contentStream = await GetResponse(path, queryParams, cancellationToken);
                Response.SearchResult result = await _jsonSerializer.DeserializeFromStreamAsync<Response.SearchResult>(contentStream);

                _logger.LogTrace($"Finish doing Search by name: {name}, count: {count}");
                
                await Task.Delay(_random.Next(4000, 10000));

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
        private string Sign(string path, string ts)
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
            // using HttpResponseMessage response = await GetAsync(url, cancellationToken);
            // using Stream content = await response.Content.ReadAsStreamAsync();

            using var response = await GetResponse(url, cancellationToken);
            var content = response.Content;

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

            // var httpClient = _httpClientFactory.CreateClient();
            // httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);

            // HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            //response.EnsureSuccessStatusCode();

            // return response;
            return null;
        }

        // TODO(Libitum): Delete this after upgrade new version of Jellyfin.
        public async Task<HttpResponseInfo> GetResponse(string url, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            /*
             * use this in new version of Jellyfin.
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);

            HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return new HttpResponseInfo()
            {
                Content = await response.Content.ReadAsStreamAsync()
            };
            */
            return await GetResponseV1(url, UserAgent, cancellationToken);
        }

        // TODO(Libitum): Delete this after upgrade new version of Jellyfin.
        public async Task<HttpResponseInfo> GetResponseV1(string url, string userAgent, CancellationToken token)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = token,
                BufferContent = true,
                UserAgent = userAgent,
            };

            var response = await _httpClient.GetResponse(options).ConfigureAwait(false);
            return response;
        }

    }
}
