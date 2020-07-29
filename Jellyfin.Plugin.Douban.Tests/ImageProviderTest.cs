using System;
using System.Threading;
using Jellyfin.Plugin.Douban.Tests.Mock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class ImageProviderTest
    {
        private readonly ImageProvider _imageProvider;
        private readonly IServiceProvider _serviceProvider;

        public ImageProviderTest()
        {
            _serviceProvider = new ServiceCollection().AddLogging(builder => builder.AddConsole())
                                                      .BuildServiceProvider();
            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ImageProvider>();

            var httpClient = new MockHttpClient();
            var jsonSerializer = new MockJsonSerializer();
            _imageProvider = new ImageProvider(httpClient, jsonSerializer, logger);
        }

        [Fact]
        public void TestGetBackdrop()
        {
            // Test 1:
            var list = _imageProvider.GetBackdrop("5350027", CancellationToken.None).Result;
            foreach (var item in list)
            {
                Console.WriteLine(item.Url);
                Console.WriteLine(item.Type);
            }
            Assert.Single(list);
        }
    }
}
