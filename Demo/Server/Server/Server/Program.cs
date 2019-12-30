using System;
using System.Diagnostics;
using System.Threading;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread thread = new Thread(StartSocket);
            thread.Start();
        }

        static void StartSocket()
        {
            ServerManager.Instance.InitServer(null, "10236");
        }
        
    }
}
