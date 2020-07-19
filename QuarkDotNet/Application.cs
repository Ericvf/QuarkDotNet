using System;

namespace QuarkDotNet
{
    public class Application
    {
        private readonly GoldleafClient goldleafClient;

        public Application(GoldleafClient goldleafClient)
        {
            this.goldleafClient = goldleafClient;
        }

        public void Run(string[] args)
        {
            goldleafClient.Start();

            Console.ReadLine();

            goldleafClient.Stop();
        }
    }
}
