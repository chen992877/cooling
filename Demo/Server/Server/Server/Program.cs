using System;
using System.Diagnostics;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerManager.Instance.InitServer(null, "10256");
            ServerManager.Instance.Close();
        }
    }
}
