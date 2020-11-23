using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Douban.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string ApiKey { get; set; }
        public int MinRequestInternalMs { get; set; }
        public PluginConfiguration()
        {
            MinRequestInternalMs = 2000;
        }
    }
}
