using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Douban
{
    public class DoubanAccessor
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        private readonly Random _random;
        // It's used to store the last access time, to reduce the access frequency.
        private static long _lastAccessTime;
        private static SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        // It's used to store the value of BID in cookie.
        private string _doubanBid;

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

        public DoubanAccessor(IHttpClient client, ILogger logger)
        {
            _httpClient = client;
            _logger = logger;
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
                //UserAgent = _userAgentList[_random.Next(_userAgentList.Length)],
                UserAgent = _userAgentList[0],
            };

            if (!string.IsNullOrEmpty(_doubanBid))
            {
                options.RequestHeaders.Add("Cookie", String.Format("bid={0}", _doubanBid));
            }

            using var response = await _httpClient.GetResponse(options).ConfigureAwait(false);

            // For test
            foreach (var h in response.Headers)
            {
                _logger.LogError("Douban header key:{0}", h.Key);
            }

            if (response.Headers.TryGetValues("X-DOUBAN-NEWBID", out IEnumerable<string> value))
            {
                _logger.LogError("Douban bid is: {0}", value.FirstOrDefault());
                _doubanBid = value.FirstOrDefault();
            }

            using var reader = new StreamReader(response.Content);
            String content = reader.ReadToEnd();

            
            return content;
        }

        // Delays for some time to reduce the access frequency.
        public async Task<String> GetResponseWithDelay(string url, CancellationToken cancellationToken)
        {
            await _locker.WaitAsync();
            try
            {
                // Check the time diff to avoid high frequency, which could lead blocked by Douban.
                long time_diff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _lastAccessTime;
                if (time_diff <= 2)
                {
                    // Use a random delay to avoid been blocked.
                    int delay = _random.Next(1500, 4000);
                    await Task.Delay(delay, cancellationToken);
                }

                var content = await GetResponse(url, cancellationToken);
                // Update last access time to now.
                _lastAccessTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return content;
            }
            finally
            {
                _locker.Release();
            }
        }
    }
}
