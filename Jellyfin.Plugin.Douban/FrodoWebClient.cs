using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
    public sealed class FrodoWebClient : IDoubanClient
    {

        private const string BaseDoubanUrl = "https://frodo.douban.com";

        /// API key to use when performing an API call.
        private const string ApiKey = "054022eaeae0b00e0fc068c0c0a2102a";

        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 "
            + "(KHTML, like Gecko) Chrome/40.0.2214.93 Safari/537.36";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        public FrodoWebClient(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer,
            ILogger logger)
        {
            this._httpClientFactory = httpClientFactory;
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
            _logger.LogInformation($"Start to Search by name: {name}, count: {count}");

            const string path = "/api/v2/search";
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

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);

            HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}
