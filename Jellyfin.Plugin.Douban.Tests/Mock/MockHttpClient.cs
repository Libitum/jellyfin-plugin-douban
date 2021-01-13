using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using ServiceStack.Text;

namespace Jellyfin.Plugin.Douban.Tests.Mock
{
    public class MockHttpClient : IHttpClient
    {
        public readonly HttpClient _httpClient;

        public MockHttpClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<HttpResponseInfo> GetResponse(HttpRequestOptions options)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
            if (options.RequestHeaders.ContainsKey("Cookie"))
            {
                _httpClient.DefaultRequestHeaders.Add("Cookie",
                                                      options.RequestHeaders.Get("Cookie"));
            }

            HttpResponseMessage response = await _httpClient.GetAsync(options.Url,
                                                                      options.CancellationToken);
            response.EnsureSuccessStatusCode();
            Stream content = await response.Content.ReadAsStreamAsync();

            HttpResponseInfo responseInfo = new HttpResponseInfo
            {
                ContentType = response.Content.Headers.ContentType.ToString(),
                Content = content,
                StatusCode = response.StatusCode,
                Headers = response.Headers,
            };

            return responseInfo;
        }

        public Task<Stream> Get(HttpRequestOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<String> GetTempFile(HttpRequestOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseInfo> GetTempFileResponse(HttpRequestOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, string httpMethod)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, HttpMethod httpMethod)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseInfo> Post(HttpRequestOptions options)
        {
            throw new NotImplementedException();
        }
    }
}