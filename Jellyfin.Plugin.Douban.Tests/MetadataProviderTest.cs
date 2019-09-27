using System;
using System.Net.Http;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using Xunit;

using Jellyfin.Plugin.Douban;

using Jellyfin.Plugin.Douban.Tests.Mock;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class MetadataProviderTest
    {
        private readonly MetadataProvider _doubanProvider;
        private readonly IServiceProvider _serviceProvider;

        public MetadataProviderTest()
        {
            _serviceProvider = new ServiceCollection().AddLogging(builder => builder.AddConsole())
                                                      .BuildServiceProvider();
            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("test");

            var httpClient = new MockHttpClient();
            var jsonSerializer = new MockJsonSerializer();
            _doubanProvider = new MetadataProvider(httpClient, jsonSerializer, logger);
        }

        [Fact]
        public void TestGetSidByName()
        {
            // var response = _doubanProvider.SearchSidByName("Inception", CancellationToken.None);
            // Assert.Equal("3541415", response.Result);
        }

        [Fact]
        public void TestGetMovieItem()
        {
            var response = _doubanProvider.GetMovieItem("3541415", CancellationToken.None);
            var metadata = response.Result;
            Assert.True(metadata.HasMetadata);
            Assert.Equal("盗梦空间", metadata.Item.Name);

            // Test not found
            Assert.ThrowsAsync<HttpRequestException>(() => 
                    _doubanProvider.GetMovieItem("23434523452", CancellationToken.None));
        }
    }
}
