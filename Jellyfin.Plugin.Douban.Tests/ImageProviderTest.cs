using System;
using System.Threading;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

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
        public void TestGetPrimary()
        {
            var list = _provider.GetPrimary("5350027", "movie", CancellationToken.None).Result;
            Assert.Single(list);
            foreach (var item in list)
            {
                Assert.Equal(ImageType.Primary, item.Type);
                Assert.EndsWith("p2530249558.webp", item.Url);
            }
        }

        [Fact]
        public void TestGetBackdrop()
        {
            // Test 1:
            var list = _provider.GetBackdrop("5350027", CancellationToken.None).Result;
            foreach (var item in list)
            {
                Console.WriteLine(item.Url);
                Assert.Equal(ImageType.Backdrop, item.Type);
            }
            Assert.Single(list);
        }
    }
}
