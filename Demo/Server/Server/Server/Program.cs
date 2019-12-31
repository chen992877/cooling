using System;
using System.Diagnostics;
using System.Threading;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerManager.Instance.InitServer("127.0.0.1", "10236");
            Thread threadUpdate = new Thread(Update);
            threadUpdate.Start();
        }

        static int i = 0;
        static void Update()
        {
            while (true)
            {
                ServerManager.Instance.Update();
                Thread.Sleep(10);
            }
        }
        
    }
}
