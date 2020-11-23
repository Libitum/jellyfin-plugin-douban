using System;
using System.Threading;
using Jellyfin.Plugin.Douban.Tests.Mock;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class TvProviderTest
    {
        private readonly TVProvider _doubanProvider;
        private readonly IServiceProvider _serviceProvider;

        public TvProviderTest()
        {
            _serviceProvider = new ServiceCollection().AddLogging(builder => builder.AddConsole())
                                                      .BuildServiceProvider();
            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<TVProvider>();

            var httpClient = new MockHttpClient();
            var jsonSerializer = new MockJsonSerializer();
            _doubanProvider = new TVProvider(httpClient, jsonSerializer, logger);
        }

        [Fact]
        public void TestSearchSeries()
        {
            SeriesInfo info = new SeriesInfo()
            {
                Name = "老友记",
            };
            var result = _doubanProvider.GetSearchResults(info, CancellationToken.None).Result;

            Assert.NotEmpty(result);
        }

        [Fact]
        public void TestGetEpisodeMetadata()
        {
            EpisodeInfo episodeInfo = new EpisodeInfo()
            {
                Name = "老友记 第一季",
                ParentIndexNumber = 1,
                IndexNumber = 1,
            };

            episodeInfo.SeriesProviderIds[FrodoUtils.ProviderId] = "1393859";
            var metadataResult = _doubanProvider.GetMetadata(episodeInfo, CancellationToken.None).Result;
            Assert.True(metadataResult.HasMetadata);

            EpisodeInfo episodeInfo2 = new EpisodeInfo()
            {
                Name = "老友记 第一季",
                ParentIndexNumber = 1,
                IndexNumber = 2,
            };

            episodeInfo2.SeriesProviderIds[FrodoUtils.ProviderId] = "1393859";
            var metadataResult2 = _doubanProvider.GetMetadata(episodeInfo2, CancellationToken.None).Result;
            Assert.True(metadataResult2.HasMetadata);
        }

        [Fact]
        public void TestGetSeasonMetadata()
        {
            SeasonInfo seasonInfo  = new SeasonInfo()
            {
                Name = "老友记 第二季"
            };
            seasonInfo.SeriesProviderIds[FrodoUtils.ProviderId] = "1393859";
            var metadataResult = _doubanProvider.GetMetadata(seasonInfo, CancellationToken.None).Result;

            Assert.True(metadataResult.HasMetadata);
        }
    }
}
