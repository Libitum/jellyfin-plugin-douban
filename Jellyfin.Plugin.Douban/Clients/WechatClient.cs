using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Douban.Clients
{
    /// <summary>
    /// Mock as Douban Wechat micro-app cliend.
    /// </summary>
    public sealed class WechatClient : IDoubanClient
    {

        private const string BaseDoubanUrl = "https://frodo.douban.com";
        /// API key to use when performing an API call.
        private const string ApiKey = "054022eaeae0b00e0fc068c0c0a2102a";
        private const string UserAgent = "MicroMessenger/";
        private const string Referer = "https://servicewechat.com/wx2f9b06c1de1ccfca/91/page-frame.html";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;

        public WechatClient(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            this._httpClientFactory = httpClientFactory;
            this._logger = logger;
        }

        /// <summary>
        /// Gets one movie or tv item by doubanID.
        /// </summary>
        /// <param name="doubanID">The subject ID in Douban.</param>
        /// <param name="type">Subject type.</param>
        /// <param name="cancellationToken">Used to cancel the request.</param>
        /// <returns>The subject of one item.</returns>
        public async Task<Response.Subject> GetSubject(string doubanID, DoubanType type, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start to GetSubject by Id: {doubanID}", doubanID);

            string path = $"/api/v2/{type:G}/{doubanID}";
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            var content = await GetResponse(path, queryParams, cancellationToken);
            JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
            Response.Subject subject = await content.ReadFromJsonAsync<Response.Subject>(options, cancellationToken);
            _logger.LogTrace("Finish doing GetSubject by Id: {doubanID}", doubanID);
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
            _logger.LogInformation($"Start to Search by name: {name}, count: {count}");

            const string path = "/api/v2/search";
            Dictionary<string, string> queryParams = new Dictionary<string, string>
            {
                { "q", name },
                { "count", count.ToString() }
            };
            var content = await GetResponse(path, queryParams, cancellationToken);
            JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
            Response.SearchResult result = await content.ReadFromJsonAsync<Response.SearchResult>(options, cancellationToken);

            _logger.LogTrace($"Finish doing Search by name: {name}, count: {count}");
            return result;
        }

        /// <summary>
        /// Sends request to Douban Frodo and get the response.
        /// It generates the signature for douban api internally.
        /// </summary>
        /// <param name="path">Douban api path, e.g. /api/v2/search/movie</param>
        /// <param name="queryParams">Parameters for the request.</param>
        /// <param name="cancellationToken">Used to cancel the request.</param>
        /// <returns>The HTTP content with the type of stream.</returns>
        private async Task<HttpContent> GetResponse(string path, Dictionary<string, string> queryParams,
                CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Start to request path: {path}");

            cancellationToken.ThrowIfCancellationRequested();

            queryParams.Add("apikey", ApiKey);

            // Generate the URL.
            string queryStr = string.Join('&', queryParams.Select(item => $"{item.Key}={HttpUtility.UrlEncode(item.Value)}"));
            string url = $"{BaseDoubanUrl}{path}?{queryStr}";
            _logger.LogInformation($"Frodo request URL: {url}");

            // Send request to Frodo API and get response.
            HttpResponseMessage response = await GetAsync(url, cancellationToken);
            _logger.LogTrace($"Finish doing request path: {path}");
            return response.Content;
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

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
            httpClient.DefaultRequestHeaders.Add("Referer", Referer);

            HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}
