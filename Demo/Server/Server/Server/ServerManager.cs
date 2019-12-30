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
                        ReadCliented(s);
                    }
                }
            }
        }

        static void ReadListened(Socket listenfd)
        {
            Console.WriteLine("accept client");
            Socket clientfd = listenfd.Accept();
            Client state = new Client();
            state.socket = clientfd;
            clients.Add(clientfd, state);
        }
        static bool ReadCliented(Socket client)
        {
            Client state = clients[client];
            int count;
            try
            {
                count = client.Receive(state.readBuff, state.buffCount, 1024 - state.buffCount, 0);
                state.buffCount += count;
            }
            catch (SocketException ex)
            {
                client.Close();
                clients.Remove(client);
                Console.WriteLine("client receive fail");
                return false;
            }
            if (count == 0)
            {
                client.Close();
                clients.Remove(client);
                Console.WriteLine("client receive fail");
                return false;
            }
            OnReceiveData(state);
            return true;
        }
        static void OnReceiveData(Client state)
        {
            Socket client = state.socket;
            int buffCount = state.buffCount;
            if (buffCount < 2)
            {
                return;
            }
            int bodyLength = BitConverter.ToInt16(state.readBuff, 0);//从第一位开始，取前两个            
            if (buffCount < 2 + bodyLength)
            {
                return;
            }
            string recStr = Encoding.Default.GetString(state.readBuff, 2, bodyLength);
            Console.WriteLine("收到消息:"+ recStr);
            foreach (Client s in clients.Values)
            {
                byte[] sendBytes = Encoding.Default.GetBytes(recStr);
                s.socket.Send(sendBytes);
                Console.WriteLine("向客户端发送" + recStr);
            }            
            //继续处理剩余字节            
            int start = 2 + bodyLength;
            int count = buffCount - start;
            Array.Copy(state.readBuff, start, state.readBuff, 0, count);
            state.buffCount -= start;
            OnReceiveData(state);
        }

        public void Close()
        {
            if (listened != null)
                listened.Close();
        }
    }
}
