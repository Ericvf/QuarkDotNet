using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QuarkDotNet.Core;

namespace QuarkDotNet.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceProvider = BuildServiceProvider();
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private ServiceProvider BuildServiceProvider()
            => new ServiceCollection()
                .AddTransient<IMainViewModel, MainViewModel>()
                .AddSingleton<MainWindow>()
                .AddSingleton<GoldleafClient>()
                .AddSingleton<UsbDeviceService>()
                .AddSingleton<FileSystem>()
                .AddSingleton<ILogger, ViewModelLogger>()
                .BuildServiceProvider();
    }
}
