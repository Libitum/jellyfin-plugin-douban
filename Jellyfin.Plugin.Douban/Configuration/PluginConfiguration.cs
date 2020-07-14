using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Douban.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string ApiKey { get; set; }
        public int MinRequestInternalMs { get; set; }
        public PluginConfiguration()
        {
            ApiKey = "0df993c66c0c636e29ecbb5344252a4a";
            MinRequestInternalMs = 2000;
        }
    }
}
