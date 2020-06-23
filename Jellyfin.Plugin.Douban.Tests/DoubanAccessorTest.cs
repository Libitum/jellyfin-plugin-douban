using System;
using System.Threading;
using Jellyfin.Plugin.Douban.Tests.Mock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class DoubanAccessorTest
    {
        private readonly DoubanAccessor _accessor;
        private ILogger _logger;

        public DoubanAccessorTest()
        {
            var httpClient = new MockHttpClient();

            _logger = new ServiceCollection()
                           .AddLogging(builder => builder.AddConsole())
                           .BuildServiceProvider()
                           .GetRequiredService<ILoggerFactory>()
                           .CreateLogger("test");

            _accessor = DoubanAccessor.Instance;
            _accessor.init(httpClient, _logger);
        }

        [Fact]
        public void TestGetResponseWithDelay()
        {
            var url = "https://www.douban.com/search?q=%E9%BE%99%E7%8C%AB";
            _ = _accessor.GetResponseWithDelay(url, CancellationToken.None).Result;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _ = _accessor.GetResponseWithDelay(url, CancellationToken.None).Result;
            long timestamp_now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _logger.LogWarning("time diff: {0}", timestamp_now - timestamp);
            Assert.True(timestamp_now - timestamp > 1500);
        }
    }
}
