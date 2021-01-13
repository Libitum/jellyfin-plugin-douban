using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
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

        [Fact]
        public void TestGetMovieItem()
        {
            // Test for right case.
            Response.Subject item = _client.GetSubject("1291561", MediaType.movie, CancellationToken.None).Result;
            Assert.Equal("1291561", item.Id);
            Assert.False(item.Is_Tv);
            Assert.Equal("千与千寻", item.Title);

            // Test if the type of subject is error.
            Assert.ThrowsAsync<HttpRequestException>(
                () => _client.GetSubject("3016187", MediaType.movie, CancellationToken.None));
        }

        [Fact]
        public void TestGetTvItem()
        {
            // Test for right case.
            Response.Subject item = _client.GetSubject("3016187", MediaType.tv, CancellationToken.None).Result;
            Assert.Equal("3016187", item.Id);
            Assert.True(item.Is_Tv);
            Assert.Equal("权力的游戏 第一季", item.Title);

            // Test if the type of subject is error.
            Assert.ThrowsAsync<HttpRequestException>(
                () => _client.GetSubject("1291561", MediaType.tv, CancellationToken.None));
        }

        [Fact]
        public void TestSearch()
        {
            // Test search movie.
            Response.SearchResult result = _client.Search("权力的游戏 第一季", CancellationToken.None).Result;
            Assert.Equal(5, result.Items.Count);
            Assert.Equal("tv", result.Items[0].Target_Type);
            Assert.Equal("3016187", result.Items[0].Target.Id);

            // Test search TV.
            result = _client.Search("千与千寻", CancellationToken.None).Result;
            Assert.Equal(5, result.Items.Count);
            Assert.Equal("movie", result.Items[0].Target_Type);
            Assert.Equal("1291561", result.Items[0].Target.Id);

            // Test not found.
            result = _client.Search("abceasd234asd", CancellationToken.None).Result;
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
        }
    }
}
