using System;
using System.Collections.Generic;
using System.Text;
using Jellyfin.Plugin.Douban.Tests.Mock;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Jellyfin.Plugin.Douban.Tests
{
    class ServiceUtils
    {
        public static ServiceProvider BuildServiceProvider<T>(ITestOutputHelper output) where T: class
        {
            var serviceProvider = new ServiceCollection()
                .AddHttpClient()
                .AddLogging(builder => builder.AddXUnit(output).SetMinimumLevel(LogLevel.Trace))
                .AddSingleton<IJsonSerializer, MockJsonSerializer>()
                .AddSingleton<T>()
                .BuildServiceProvider();

            return serviceProvider;
        }
    }
}
