using Jellyfin.Plugin.Douban.Response;

namespace Jellyfin.Plugin.Douban
{
    public sealed class SubjectCache
    {
        private static readonly SubjectCache instance = new SubjectCache();

        public Subject subject { get; set; }

        static SubjectCache()
        {
        }

        private SubjectCache()
        {
        }

        public bool Has(string id)
        {
            return subject?.Id == id;
        }

        public static SubjectCache Instance
        {
            get
            {
                return instance;
            }
        }
    }
}