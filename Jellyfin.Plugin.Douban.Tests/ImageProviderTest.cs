using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

using Jellyfin.Plugin.Douban.Providers;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class ImageProviderTest
    {
        private readonly ImageProvider _provider;

        public ImageProviderTest(ITestOutputHelper output)
        {
            var serviceProvider = ServiceUtils.BuildServiceProvider<ImageProvider>(output);
            _provider = serviceProvider.GetService<ImageProvider>();
        }


        [Fact]
        public async Task TestGetPrimary()
        {
            var list = await _provider.GetPrimary("5350027", "movie", CancellationToken.None);
            Assert.Single(list);
            foreach (var item in list)
            {
                Assert.Equal(ImageType.Primary, item.Type);
                Assert.EndsWith("p2530249558.jpg", item.Url);
            }
        }

        [Fact]
        public async Task TestGetBackdrop()
        {
            // Test 1:
            var list = await _provider.GetBackdrop("5350027", CancellationToken.None);
            foreach (var item in list)
            {
                Console.WriteLine(item.Url);
                Assert.Equal(ImageType.Backdrop, item.Type);
            }
            Assert.Single(list);
        }
    }
}
