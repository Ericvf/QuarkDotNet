using Microsoft.Extensions.DependencyInjection;
using QuarkDotNet.Core;

namespace QuarkDotNet
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = BuildServiceProvider();
            var application = serviceProvider.GetRequiredService<Application>();
            application.Run(args);
        }

        private static ServiceProvider BuildServiceProvider() 
            => new ServiceCollection()
                .AddSingleton<Application>()
                .AddSingleton<GoldleafClient>()
                .AddSingleton<UsbDeviceService>()
                .AddSingleton<FileSystem>()
                .AddSingleton<Settings>()
                .AddSingleton<ILogger, ConsoleLogger>()
                .BuildServiceProvider();
    }
}
