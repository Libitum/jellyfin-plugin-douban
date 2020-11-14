using System;
using System.Threading;
using Jellyfin.Plugin.Douban.Tests.Mock;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class MovieProviderTest
    {
        private readonly MovieProvider _doubanProvider;
        private readonly IServiceProvider _serviceProvider;

        public MovieProviderTest()
        {
            _serviceProvider = new ServiceCollection().AddLogging(builder => builder.AddConsole())
                                                      .BuildServiceProvider();
            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<MovieProvider>();

            var httpClient = new MockHttpClient();
            var jsonSerializer = new MockJsonSerializer();
            _doubanProvider = new MovieProvider(httpClient, jsonSerializer, logger);
        }

        [Fact]
        public void TestSearchMovie()
        {
            // Test 1: search metadata.
            MovieInfo info = new MovieInfo()
            {
                Name = "蝙蝠侠：黑暗骑士",
            };

            var result = _doubanProvider.GetSearchResults(info, CancellationToken.None).Result;
            Assert.NotEmpty(result);
        }

        [Fact]
        public void TestGetMovieMetadata()
        {
            MovieInfo info = new MovieInfo()
            {
                Name = "Source Code"
            };
            var meta = _doubanProvider.GetMetadata(info, CancellationToken.None).Result;
            Assert.True(meta.HasMetadata);
            Assert.Equal("源代码", meta.Item.Name);
        }
    }
}
