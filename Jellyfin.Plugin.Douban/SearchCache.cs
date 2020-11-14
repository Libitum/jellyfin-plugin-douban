using System.Collections.Generic;
using Jellyfin.Plugin.Douban.Response;

namespace Jellyfin.Plugin.Douban
{
    public sealed class SearchCache
    {
        private static readonly SearchCache instance = new SearchCache();

        public List<SearchTarget> searchResult { get; set; }

        private string searchId { get; set; }

        static SearchCache()
        {
        }

        private SearchCache()
        {
        }

        public bool Has(string id)
        {
            return searchId == id;
        }

        public void SetSearchCache(string id, List<SearchTarget> result)
        {
            searchId = id;
            searchResult = result;
        }

        public static SearchCache Instance
        {
            get
            {
                return instance;
            }
        }
    }
}