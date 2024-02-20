using System.Threading;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

using Jellyfin.Plugin.Douban.Providers;

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
        public async void TestSearchSeries()
        {
            SeriesInfo info = new SeriesInfo()
            {
                Name = "老友记",
            };
            var result = await _doubanProvider.GetSearchResults(info, CancellationToken.None);

            Assert.NotEmpty(result);
        }

        [Fact]
        public async void TestGetEpisodeMetadata()
        {
            EpisodeInfo episodeInfo = new EpisodeInfo()
            {
                Name = "老友记 第一季",
                ParentIndexNumber = 1,
                IndexNumber = 1,
            };

            episodeInfo.SeriesProviderIds["DoubanID"] = "1393859";
            var metadataResult = await _doubanProvider.GetMetadata(episodeInfo, CancellationToken.None);
            Assert.True(metadataResult.HasMetadata);

            EpisodeInfo episodeInfo2 = new EpisodeInfo()
            {
                Name = "老友记 第一季",
                ParentIndexNumber = 1,
                IndexNumber = 2,
            };

            episodeInfo2.SeriesProviderIds["DoubanID"] = "1393859";
            var metadataResult2 = await _doubanProvider.GetMetadata(episodeInfo2, CancellationToken.None);
            Assert.True(metadataResult2.HasMetadata);
        }

        [Fact]
        public async void TestGetSeasonMetadata()
        {
            SeasonInfo seasonInfo = new SeasonInfo()
            {
                Name = "老友记 第二季"
            };
            seasonInfo.SeriesProviderIds["DoubanID"] = "1393859";
            var metadataResult = await _doubanProvider.GetMetadata(seasonInfo, CancellationToken.None);

            Assert.True(metadataResult.HasMetadata);
        }
    }
}
