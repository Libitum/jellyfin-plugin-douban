using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Plugin.Douban.Tests
{
    public class LRUCacheTest
    {
        private LRUCache cache;

        [Fact]
        public void TestAdd()
        {
            cache = new LRUCache(2);
            cache.Add("1", "1");
            cache.Add("2", "2");
            cache.Add("3", "3");

            // "1" should not exist.
            Assert.False(cache.TryGet<string>("1", out string value));
            Assert.True(cache.TryGet<string>("2", out value));
            Assert.Equal("2", value);
            Assert.True(cache.TryGet<string>("3", out value));
            Assert.Equal("3", value);

            cache.Add("4", "4");
            Assert.False(cache.TryGet<string>("2", out value));
            Assert.True(cache.TryGet<string>("4", out value));
            Assert.Equal("4", value);
        }

        [Fact]
        public void TestLRU1()
        {
            cache = new LRUCache(2);
            cache.Add("1", "1");
            cache.Add("2", "2");
            cache.Add("3", "3");

            Assert.True(cache.TryGet<string>("2", out string value));
            Assert.Equal("2", value);

            cache.Add("4", "4");

            Assert.False(cache.TryGet<string>("3", out _));

            Assert.True(cache.TryGet<string>("2", out value));
            Assert.Equal("2", value);

            Assert.True(cache.TryGet<string>("4", out value));
            Assert.Equal("4", value);
        }
    }
}
