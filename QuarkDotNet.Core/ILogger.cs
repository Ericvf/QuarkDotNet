namespace QuarkDotNet.Core
{
    public interface ILogger
    {
        void Debug(string message);

        void Print(string message);

        void Error(string message);
    }
}
