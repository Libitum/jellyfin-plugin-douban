using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit.Abstractions;

namespace Jellyfin.Plugin.Douban.Tests
{
    class ServiceUtils
    {
        public static ServiceProvider BuildServiceProvider<T>(ITestOutputHelper output) where T : class
        {
            var services = new ServiceCollection()
                .AddHttpClient()
                //.AddLogging(builder => builder.AddXUnit(output).SetMinimumLevel(LogLevel.Debug))
                .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddSingleton<T>();

            var serviceProvider = services.BuildServiceProvider();

            // Used For FrodoAndroidClient which can not use typed ILogger.
            var logger = serviceProvider.GetService<ILogger<T>>();
            services.AddSingleton<ILogger>(logger);

            return services.BuildServiceProvider();
        }
    }
}
