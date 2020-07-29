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
        public void TestGetMetadata()
        {
            MovieInfo info = new MovieInfo()
            {
                Name = "龙猫",
                MetadataLanguage = "en",
            };

            // Test 1: language is not "zh"
            var meta = _doubanProvider.GetMetadata(info, CancellationToken.None).Result;
            Assert.False(meta.HasMetadata);

            // Test 2: can not get the result.
            info = new MovieInfo()
            {
                MetadataLanguage = "zh",
                Name = "asdflkjhsadf"
            };
            meta = _doubanProvider.GetMetadata(info, CancellationToken.None).Result;
            Assert.False(meta.HasMetadata);

            // Test 3: get meta successfully
            info = new MovieInfo()
            {
                MetadataLanguage = "zh",
                Name = "龙猫"
            };
            meta = _doubanProvider.GetMetadata(info, CancellationToken.None).Result;
            Assert.True(meta.HasMetadata);
            Assert.Equal("龙猫", meta.Item.Name);

            // Test 4: get it but it's not movie type
            info = new MovieInfo()
            {
                MetadataLanguage = "zh",
                Name = "三国演义"
            };
            meta = _doubanProvider.GetMetadata(info, CancellationToken.None).Result;
            Assert.False(meta.HasMetadata);
        }
    }
}
