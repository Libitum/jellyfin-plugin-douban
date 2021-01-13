using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class MovieProviderTest
    {
        private readonly MovieProvider _provider;
        public MovieProviderTest(ITestOutputHelper output)
        {
            var serviceProvider = ServiceUtils.BuildServiceProvider<MovieProvider>(output);
            _provider = serviceProvider.GetService<MovieProvider>();
        }

        [Fact]
        public void TestGetSearchResults()
        {
            // Test 1: search metadata.
            MovieInfo info = new MovieInfo()
            {
                Name = "蝙蝠侠.黑暗骑士",
            };

            var result = _provider.GetSearchResults(info, CancellationToken.None).Result;
            Assert.NotEmpty(result);
            Assert.True(result.Count() > 1);
            string doubanId = result.FirstOrDefault()?.GetProviderId(BaseProvider.ProviderID);
            int? year = result.FirstOrDefault()?.ProductionYear;
            Assert.Equal("1851857", doubanId);
            Assert.Equal(2008, year);

            // Test 2: Already has provider Id.
            info.SetProviderId(BaseProvider.ProviderID, "1851857");
            result = _provider.GetSearchResults(info, CancellationToken.None).Result;
            Assert.True(result.Count() == 1);
            doubanId = result.FirstOrDefault()?.GetProviderId(BaseProvider.ProviderID);
            year = result.FirstOrDefault()?.ProductionYear;
            Assert.Equal("1851857", doubanId);
            Assert.Equal(2008, year);
        }

        [Fact]
        public void TestGetMetadata()
        {
            // Test 1: Normal case.
            MovieInfo info = new MovieInfo()
            {
                Name = "Source Code"
            };
            var meta = _provider.GetMetadata(info, CancellationToken.None).Result;
            Assert.True(meta.HasMetadata);
            Assert.Equal("源代码", meta.Item.Name);
            Assert.Equal("3075287", meta.Item.GetProviderId(BaseProvider.ProviderID));
            Assert.Equal(DateTime.Parse("2011-08-30"), meta.Item.PremiereDate);

            // Test 2: Already has provider Id.
            info = new MovieInfo()
            {
                Name = "Source Code"
            };
            info.SetProviderId(BaseProvider.ProviderID, "1851857");
            meta = _provider.GetMetadata(info, CancellationToken.None).Result;
            Assert.True(meta.HasMetadata);
            Assert.Equal("蝙蝠侠：黑暗骑士", meta.Item.Name);

            // Test 2: Not movie type.
            info = new MovieInfo()
            {
                Name = "大秦帝国"
            };
            meta = _provider.GetMetadata(info, CancellationToken.None).Result;
            Assert.False(meta.HasMetadata);
        }
    }
}
