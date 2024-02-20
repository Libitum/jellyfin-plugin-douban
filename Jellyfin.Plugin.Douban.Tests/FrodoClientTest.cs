using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Jellyfin.Plugin.Douban.Clients;

using Xunit;
using Xunit.Abstractions;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class FrodoClientTest
    {
        private readonly FrodoAndroidClient _client;

        public FrodoClientTest(ITestOutputHelper output)
        {
            var serviceProvider = ServiceUtils.BuildServiceProvider<FrodoAndroidClient>(output);
            _client = serviceProvider.GetService<FrodoAndroidClient>();
        }

        //[Fact]
        public async void TestGetMovieItem()
        {
            // Test for right case.
            Response.Subject item = await _client.GetSubject("1291561", DoubanType.movie, CancellationToken.None);
            Assert.Equal("1291561", item.Id);
            Assert.False(item.Is_Tv);
            Assert.Equal("千与千寻", item.Title);

            // Test if the type of subject is error.
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _client.GetSubject("3016187", DoubanType.movie, CancellationToken.None));

            // For cache
            item = await _client.GetSubject("1291561", DoubanType.movie, CancellationToken.None);
            Assert.Equal("千与千寻", item.Title);
        }

        // [Fact]
        public async void TestGetTvItem()
        {
            // Test for right case.
            Response.Subject item = await _client.GetSubject("3016187", DoubanType.tv, CancellationToken.None);
            Assert.Equal("3016187", item.Id);
            Assert.True(item.Is_Tv);
            Assert.Equal("权力的游戏 第一季", item.Title);

            // Test if the type of subject is error.
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _client.GetSubject("1291561", DoubanType.tv, CancellationToken.None));
        }

        //[Fact]
        public async Task TestSearch()
        {
            // Test search movie.
            Response.SearchResult result = await _client.Search("权力的游戏 第一季", CancellationToken.None);
            Assert.Equal(5, result.Subjects.Items.Count);
            Assert.Equal("tv", result.Subjects.Items[0].Target_Type);
            Assert.Equal("3016187", result.Subjects.Items[0].Target.Id);

            // Test search TV.
            result = await _client.Search("千与千寻", CancellationToken.None);
            Assert.Equal(5, result.Subjects.Items.Count);
            Assert.Equal("movie", result.Subjects.Items[0].Target_Type);
            Assert.Equal("1291561", result.Subjects.Items[0].Target.Id);

            // Test not found.
            result = await _client.Search("abceasd234asd", CancellationToken.None);
            Assert.Empty(result.Subjects.Items);
        }
    }
}
