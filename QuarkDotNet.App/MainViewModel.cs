using QuarkDotNet.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace QuarkDotNet.App
{
    public class MainViewModel : IMainViewModel, INotifyPropertyChanged
    {
        static readonly ImageSource connectedIcon = new BitmapImage(new Uri("pack://application:,,,/connected.ico"));
        static readonly ImageSource disconnectedIcon = new BitmapImage(new Uri("pack://application:,,,/disconnected.ico"));
        static readonly ObservableCollection<string> logHistory = new ObservableCollection<string>();
        private readonly ObservableCollection<string> resourceFiles = new ObservableCollection<string>();

        private readonly Settings settings;
        private readonly FileSystem fileSystem;
        private readonly NspParser nspParser;

        public ObservableCollection<string> LogHistory => logHistory;

        public ObservableCollection<string> ResourceFiles => resourceFiles;

        public MainViewModel(Settings settings, FileSystem fileSystem, NspParser nspParser)
        {
            this.settings = settings;
            this.fileSystem = fileSystem;
            this.nspParser = nspParser;
        }

        #region Properties

        public WindowState WindowState
        {
            get { return windowState; }
            set
            {
                if (windowState != value)
                {
                    windowState = value;
                    OnPropertyChanged(WindowStatePropertyName);
                }
            }
        }
        private WindowState windowState;
        public const string WindowStatePropertyName = "WindowState";

        public bool IsConnected
        {
            get { return isConnected; }
            set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    OnPropertyChanged(IsConnectedPropertyName);
                    Image = isConnected ? connectedIcon : disconnectedIcon;
                }
            }
        }
        private bool isConnected;
        public const string IsConnectedPropertyName = "IsConnected";

        public ImageSource Image
        {
            get { return _Image ?? disconnectedIcon; }
            set
            {
                if (_Image != value)
                {
                    _Image = value;
                    OnPropertyChanged(ImagePropertyName);
                }
            }
        }
        private ImageSource _Image;
        public const string ImagePropertyName = "Image";

        public bool IsLogVisible
        {
            get { return _IsLogVisible; }
            set
            {
                if (_IsLogVisible != value)
                {
                    _IsLogVisible = value;
                    OnPropertyChanged(IsLogVisiblePropertyName);
                }
            }
        }
        private bool _IsLogVisible;
        public const string IsLogVisiblePropertyName = "IsLogVisible";
        #endregion

        #region Commands 

        public ICommand LeftClick => leftClickCommand;
        public ICommand leftClickCommand => new RelayCommand(_ => ToggleMinimized());

        #endregion

        private void ToggleMinimized()
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Minimized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        public void GoldLeafClientStateChange(object sender, GoldleafClient.State e)
            => IsConnected = e.IsConnected;

        public void Load()
        {
            var model = settings.Get();
            IsLogVisible = model.IsLogVisible;

            var nspFiles =
                from path in model.Paths
                from file in fileSystem.GetAllFiles(path, "*.*")
                where file.EndsWith(".nsp", StringComparison.OrdinalIgnoreCase)
                let nspModel = nspParser.Parse(file)
                select nspModel;

            ResourceFiles.Clear();
            foreach (var nspModel in nspFiles)
            {
                ResourceFiles.Add($"{nspModel.Name} ({nspModel.Size})");
            }
        }

        public void Add(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(() => logHistory.Add(message));
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public interface IMainViewModel
    {
        bool IsConnected { get; set; }

        ImageSource Image { get; set; }

        void GoldLeafClientStateChange(object sender, GoldleafClient.State e);

        void Add(string message);

        void Load();
    }
}
