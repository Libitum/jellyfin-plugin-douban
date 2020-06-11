using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

namespace Jellyfin.Plugin.Douban
{
    public class DoubanAccessor
    {
        private IHttpClient _httpClient;
        private long _lastAccessTime;
        private readonly Random _random;
        
        // Used as the user agent when access Douban.
        private readonly string[] _userAgentList = {
            "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.93 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; rv:6.0) Gecko/20100101 Firefox/19.0",
            "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1464.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:22.0) Gecko/20130328 Firefox/22.0",
            "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.93 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:31.0) Gecko/20130401 Firefox/31.0",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:23.0) Gecko/20131011 Firefox/23.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_7_3) AppleWebKit/534.55.3 (KHTML, like Gecko) Version/5.1.3 Safari/534.53.10",
            "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_6_5; ar) AppleWebKit/533.19.4 (KHTML, like Gecko) Version/5.0.3 Safari/533.19.4",
            "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.93 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1467.0 Safari/537.36"
        };

        public DoubanAccessor(IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _lastAccessTime = 0;
            _random = new Random();
        }

        public async Task<String> GetResponse(string url, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true,
                UserAgent = _userAgentList[_random.Next(_userAgentList.Length)],
            };

            using var response = await _httpClient.GetResponse(options).ConfigureAwait(false);
            using var reader = new StreamReader(response.Content);
            String content = reader.ReadToEnd();

            // Update last access time to now.
            _lastAccessTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return content;
        }

        public async Task<String> GetResponseWithDelay(string url, CancellationToken cancellationToken)
        {
            // Check the time diff to avoid high frequency, which could lead blocked by Douban.
            long time_diff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _lastAccessTime;
            if (time_diff <= 2)
            {
                // Use a random delay to avoid been blocked.
                int delay = _random.Next(1000, 4000);
                await Task.Delay(delay, cancellationToken);
            }

            return await GetResponse(url, cancellationToken);
        }
    }
}
