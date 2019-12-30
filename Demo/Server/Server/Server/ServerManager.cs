using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    class Client
    {
        public Socket socket;
        public Byte[] readBuff = new byte[1024];
        public int buffCount;
        public bool ReadCliented()
        {
            int count;
            try
            {
                count = socket.Receive(readBuff, buffCount, 1024 - buffCount, 0);
                buffCount += count;
            }
            catch (SocketException ex)
            {
                socket.Close();
                ServerManager.Instance.RemoveClient(socket);
                Console.WriteLine("client receive fail");
                return false;
            }
            if (count == 0)
            {
                socket.Close();
                ServerManager.Instance.RemoveClient(socket);
                Console.WriteLine("client receive fail");
                return false;
            }
            OnReceiveData();
            return true;
        }

        void OnReceiveData()
        {
            if (buffCount < 2)
            {
                return;
            }
            int bodyLength = BitConverter.ToInt16(readBuff, 0);//从第一位开始，取前两个            
            if (buffCount < 2 + bodyLength)
            {
                return;
            }
            string recStr = Encoding.Default.GetString(readBuff, 2, bodyLength);
            ReceiveMsg(recStr);
            //继续处理剩余字节            
            int start = 2 + bodyLength;
            int count = buffCount - start;
            Array.Copy(readBuff, start, readBuff, 0, count);
            buffCount -= start;
            OnReceiveData();
        }
        public void ReceiveMsg(string str)
        {
            Console.WriteLine("收到消息:" + str);
        }

        public void PostMsg()
        {
            string msg = "Test Message";
            byte[] sendBytes = Encoding.Default.GetBytes(msg);
            socket.Send(sendBytes);
            Console.WriteLine("向客户端发送" + msg);
        }
    }
    class ServerManager: Singleton<ServerManager>
    {
        static Socket listened;
        static Dictionary<Socket, Client> clients = new Dictionary<Socket, Client>();

        public void InitServer(string ip, string port)
        {
            listened = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ipEp;
            if(!string.IsNullOrEmpty(ip))
            {
                IPAddress ipadr = IPAddress.Parse(ip);
                ipEp = new IPEndPoint(ipadr, int.Parse(port));
            }
            else
            {
                ipEp = new IPEndPoint(IPAddress.Any, int.Parse(port));
            }
            listened.Bind(ipEp);
            listened.Listen(0);
            Console.WriteLine("Socket Open Ready");
            List<Socket> checkRead = new List<Socket>();
            while (true)
            {
                checkRead.Clear();
                checkRead.Add(listened);
                foreach (Client cs in clients.Values)
                {
                    checkRead.Add(cs.socket);
                }
                Socket.Select(checkRead, null, null, 1000);
                //多路复用，检测是否有可读或可写                
                foreach (Socket s in checkRead)
                {
                    if (s == listened)
                    {
                        ReadListened(s);
                    }
                    else
                    {
                        clients[s].ReadCliented();
                    }
                }
            }
        }

        static void ReadListened(Socket listenfd)
        {
            Console.WriteLine("accept client");
            Socket cliented = listenfd.Accept();
            Client state = new Client();
            state.socket = cliented;
            clients.Add(cliented, state);
        }

        public void RemoveClient(Socket socket)
        {
            clients.Remove(socket);
        }

        public void Close()
        {
            if (listened != null)
                listened.Close();
        }
    }
}
