using System;
using QuarkDotNet.Core;

namespace QuarkDotNet
{
    public class ConsoleLogger : ILogger
    {
        public void Debug(string message) => Console.WriteLine(message);

        public void Error(string message) => Console.WriteLine(message);

        public void Print(string message) => Console.WriteLine(message);
    }
}
