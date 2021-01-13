using System.Threading;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class TvProviderTest
    {
        private readonly TVProvider _doubanProvider;
        public TvProviderTest(ITestOutputHelper output)
        {
            var serviceProvider = ServiceUtils.BuildServiceProvider<TVProvider>(output);
            _doubanProvider = serviceProvider.GetService<TVProvider>();
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

            episodeInfo.SeriesProviderIds["DoubanID"] = "1393859";
            var metadataResult = _doubanProvider.GetMetadata(episodeInfo, CancellationToken.None).Result;
            Assert.True(metadataResult.HasMetadata);

            EpisodeInfo episodeInfo2 = new EpisodeInfo()
            {
                Name = "老友记 第一季",
                ParentIndexNumber = 1,
                IndexNumber = 2,
            };

            episodeInfo2.SeriesProviderIds["DoubanID"] = "1393859";
            var metadataResult2 = _doubanProvider.GetMetadata(episodeInfo2, CancellationToken.None).Result;
            Assert.True(metadataResult2.HasMetadata);
        }

        [Fact]
        public void TestGetSeasonMetadata()
        {
            SeasonInfo seasonInfo = new SeasonInfo()
            {
                Name = "老友记 第二季"
            };
            seasonInfo.SeriesProviderIds["DoubanID"] = "1393859";
            var metadataResult = _doubanProvider.GetMetadata(seasonInfo, CancellationToken.None).Result;

            Assert.True(metadataResult.HasMetadata);
        }
    }
}
