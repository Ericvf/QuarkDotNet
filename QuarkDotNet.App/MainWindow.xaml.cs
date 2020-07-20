using System.Windows;
using System.Windows.Controls;

namespace QuarkDotNet.App
{
    public partial class MainWindow : Window
    {
        public MainWindow(IMainViewModel mainViewModel, GoldleafClient goldLeafClient)
        {
            DataContext = mainViewModel;

            Loaded += (s, e) => {
                goldLeafClient.Start();
                mainViewModel.Load();
            };
            Closing += (s, e) => goldLeafClient.Stop();

            goldLeafClient.StateChange += mainViewModel.GoldLeafClientStateChange;

            InitializeComponent();
        }

        private void ListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && e.ExtentHeightChange > 0)
                scrollViewer.ScrollToBottom();
        }
    }
}
