using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

namespace Jellyfin.Plugin.Douban.Tests.Mock
{
    public class MockHttpClient : IHttpClient
    {
        public readonly HttpClient _httpClient;

        public MockHttpClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (X11; Linux i686; rv:64.0) Gecko/20100101 Firefox/64.0");
        }

        public async Task<HttpResponseInfo> GetResponse(HttpRequestOptions options)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(options.Url,
                                                                      options.CancellationToken);
            response.EnsureSuccessStatusCode();
            Stream content = await response.Content.ReadAsStreamAsync();

            HttpResponseInfo responseInfo = new HttpResponseInfo
            {
                ContentType = response.Content.Headers.ContentType.ToString(),
                Content = content,
                StatusCode = response.StatusCode,
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