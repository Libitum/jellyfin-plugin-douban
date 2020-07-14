using System;
using System.Threading;
using Jellyfin.Plugin.Douban.Tests.Mock;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class DoubanAccessorTest
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public DoubanAccessorTest()
        {
            _httpClient = new MockHttpClient();

            _logger = new ServiceCollection()
                           .AddLogging(builder => builder.AddConsole())
                           .BuildServiceProvider()
                           .GetRequiredService<ILoggerFactory>()
                           .CreateLogger("test");            
        }

        [Fact]
        public void TestGetResponseWithDelay()
        {
            DoubanAccessor accessor = new DoubanAccessor(_httpClient, _logger);

            var url = "https://www.douban.com/search?q=%E9%BE%99%E7%8C%AB";
            _ = accessor.GetResponseWithDelay(url, CancellationToken.None).Result;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _ = accessor.GetResponseWithDelay(url, CancellationToken.None).Result;
            long timestamp_now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _logger.LogWarning("time diff: {0}", timestamp_now - timestamp);
            Assert.True(timestamp_now - timestamp > 2000);
        }

        [Fact]
        public void TestGetResponseWithNoDelay()
        {
            DoubanAccessor accessor = new DoubanAccessor(_httpClient, _logger, 0);

            var url = "https://www.douban.com/search?q=%E9%BE%99%E7%8C%AB";
            _ = accessor.GetResponseWithDelay(url, CancellationToken.None).Result;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _ = accessor.GetResponseWithDelay(url, CancellationToken.None).Result;
            long timestamp_now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _logger.LogWarning("time diff: {0}", timestamp_now - timestamp);
            Assert.True(timestamp_now - timestamp < 2000);
        }
    }
}
