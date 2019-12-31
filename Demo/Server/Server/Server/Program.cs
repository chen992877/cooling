using System;
using System.Diagnostics;
using System.Threading;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerManager.Instance.InitServer(null, "10236");
            Thread threadUpdate = new Thread(Update);
            threadUpdate.Start();
        }

        static int i = 0;
        static void Update()
        {
            while(true)
            {
                Thread.Sleep(1000);
                i++;
                Console.WriteLine(i);
            }
        }
        
    }
}
