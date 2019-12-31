using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class ByteArray
    {
        public byte[] bytes;
        public int readIndex;
        private int writeIndex;
        public int length
        { get
            {
                return writeIndex - readIndex;
            }
        }
        public ByteArray(byte[] defaultByte)
        {
            bytes = defaultByte;
            readIndex = 0;
            writeIndex = defaultByte.Length;
        }
    }

    class Client
    {
        public Socket socket;
        public Byte[] readBuff = new byte[1024];
        public int buffCount;

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
        public void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                Client state = ar.AsyncState as Client;
                Socket client = state.socket;
                int count = client.EndReceive(ar);
                if (count == 0)
                {
                    client.Close();
                    ServerManager.Instance.RemoveClient(client);
                    Console.Write("客户端关闭");
                    return;
                }
                buffCount += count;
                OnReceiveData();
                client.BeginReceive(state.readBuff, 0, 1024 - buffCount, 0, ReceiveCallBack, state);
            }
            catch (SocketException e)
            {
                Console.WriteLine("接收数据失败");
            }
        }
        Queue<ByteArray> writeQueue = new Queue<ByteArray>();
        public void Send(string msg)
        {
            if (socket == null)
                return;
            if (!socket.Connected)
                return;
            byte[] bodyBytes = Encoding.Default.GetBytes(msg);
            //协议编码        
            Int16 bodyLenth = (Int16)bodyBytes.Length;
            //计算协议长度        
            byte[] sendBytes = BitConverter.GetBytes(bodyLenth);
            //将协议长度信息转换为字节数组         
            if (!BitConverter.IsLittleEndian)//判断大小端        
            {           
                Console.WriteLine("Reverse lengthByte");
                Array.Reverse(sendBytes);
            }
            bodyBytes.CopyTo(sendBytes, sendBytes.Length);//将协议长度信息和协议信息拼接在一起，其中协议长度信息占两个字节         
            ByteArray ba = new ByteArray(sendBytes);
            lock (writeQueue)
            {
                writeQueue.Enqueue(ba);
            }
            if (writeQueue.Count == 1)
            {
                ba = writeQueue.Dequeue();
                socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallBack, socket);
            }
        }
        ByteArray ba;
        private void SendCallBack(IAsyncResult result)//异步发送协议回调    
        {
            Socket socket = (Socket)result.AsyncState;
            int count = socket.EndSend(result);

            ba.readIndex += count;//记录发送数量
            if (ba.length == 0)//发送完成则取下一条        
            {
                ba = null;
                if(writeQueue.Count > 0)
                    ba = writeQueue.Dequeue();
            }     
            
            if (ba != null)//没发送完成则记录发送        
            {
                socket.BeginSend(ba.bytes,ba.readIndex,ba.length,0,SendCallBack,socket);
            }
        }

        public void ReceiveMsg(string str)
        {
            Console.WriteLine("收到消息:" + str);
        }
    }
    class ServerManager: Singleton<ServerManager>
    {
        Socket listened;
        Dictionary<Socket, Client> clients = new Dictionary<Socket, Client>();

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

            listened.BeginAccept(AcceptCallBack, listened);
        }

        void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("客户端接入");
                Socket server = ar.AsyncState as Socket;
                Socket client = server.EndAccept(ar);
                Client clientState = new Client();
                clientState.socket = client;
                //将连接进来的客户端保存起来       
                lock(clients)
                {
                    clients.Add(client, clientState);
                    //接收此客户端发来的信息       
                    Thread startThread = new Thread(() => {
                        client.BeginReceive(clientState.readBuff, 0, 1024, 0, clientState.ReceiveCallBack, clientState);
                    });
                    startThread.Start();
                    //继续监听新的客户端接入            
                    server.BeginAccept(AcceptCallBack, server);
                }
            }
            catch (SocketException e)
            {
                Console.Write("Accept fail");
            }
        }
        
        public void RemoveClient(Socket socket)
        {
            lock (clients)
            {
                clients.Remove(socket);
            }
        }

        public void Close()
        {
            if (listened != null)
                listened.Close();
        }
    }
}
