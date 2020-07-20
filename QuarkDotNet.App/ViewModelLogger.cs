using QuarkDotNet.Core;

namespace QuarkDotNet.App
{
    public class ViewModelLogger : ILogger
    {
        private readonly IMainViewModel mainViewModel;

        public ViewModelLogger(IMainViewModel mainViewModel) => this.mainViewModel = mainViewModel;

        public void Debug(string message) => mainViewModel.Add(message);

        public void Print(string message) => mainViewModel.Add(message);

        public void Error(string message) => mainViewModel.Add(message);
    }
}
