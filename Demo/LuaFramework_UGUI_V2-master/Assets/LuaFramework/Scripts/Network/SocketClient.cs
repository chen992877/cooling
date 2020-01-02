using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Text;
using System.Collections.Generic;

public class SocketClient {
    private Socket client;
    private byte[] readBuff = new byte[1024];
    private int buffCount;
    // Use this for initialization	
    public void Init () {
        Thread thread = new Thread(ConnectToServer);
        thread.Start();
    }
    private void ConnectToServer()
    {
        client = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);   
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"),10236);
        client.BeginConnect(endPoint, OnConnectCallBack, client);
    }
    void OnConnectCallBack(IAsyncResult ar)
    {
        Socket socket = ar.AsyncState as Socket;
        socket.EndConnect(ar);
        Debug.Log("Connected sucess");
        socket.BeginReceive(readBuff, 0,1024,0, OnReiceiveCallBack, socket);
    }
    void OnReiceiveCallBack(IAsyncResult ar)
    {
        try {
            Socket socket = ar.AsyncState as Socket;
            int count = socket.EndReceive(ar);
            string recStr = Encoding.UTF8.GetString(readBuff, 0, count);
            if(count == 0)
            {
                Close();
                return;
            }
            buffCount += count;
            OnReceiveData();
            //在此处继续接收信息            
            socket.BeginReceive(readBuff, 0, 1024 - buffCount, 0, OnReiceiveCallBack, socket);
        }
        catch (SocketException e)
        {
            Debug.Log("Receive fail");
            Close();
        }
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
    Queue<String> msgQueue = new Queue<string>();
    public void ReceiveMsg(string str)
    {
        lock(msgQueue)
        {
            msgQueue.Enqueue(str);
        }
        Debug.LogError("收到消息:" + str);
    }

    public Queue<String> MsgQueue
    {
        get
        {
            return msgQueue;
        }
    }

    Queue<ByteArray> writeQueue = new Queue<ByteArray>();
    ByteArray baCur;
    public void Send(string msg)
    {
        if (client == null)
            return;
        if (!client.Connected)
            return;
        byte[] bodyBytes = Encoding.Default.GetBytes(msg);
        //协议编码        
        Int16 bodyLenth = (Int16)bodyBytes.Length;
        //计算协议长度        
        byte[] lengthBytes = BitConverter.GetBytes(bodyLenth);
        //将协议长度信息转换为字节数组         
        if (!BitConverter.IsLittleEndian)//判断大小端        
        {
            Console.WriteLine("Reverse lengthByte");
            Array.Reverse(lengthBytes);
        }
        byte[] sendBytes = new byte[bodyBytes.Length + lengthBytes.Length];
        lengthBytes.CopyTo(sendBytes, 0);
        bodyBytes.CopyTo(sendBytes, lengthBytes.Length);  
        ByteArray ba = new ByteArray(sendBytes);
        lock (writeQueue)
        {
            writeQueue.Enqueue(ba);
        }
        if (writeQueue.Count == 1)
        {
            baCur = writeQueue.Dequeue();
            client.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallBack, client);
        }
    }
    private void SendCallBack(IAsyncResult result)//异步发送协议回调    
    {
        Socket socket = (Socket)result.AsyncState;
        int count = socket.EndSend(result);

        baCur.readIndex += count;//记录发送数量
        if (baCur.length == 0)//发送完成则取下一条        
        {
            baCur = null;
            if (writeQueue.Count > 0)
                baCur = writeQueue.Dequeue();
        }

        if (baCur != null)//没发送完成则记录发送        
        {
            socket.BeginSend(baCur.bytes, baCur.readIndex, baCur.length, 0, SendCallBack, socket);
        }
    }

    public void Close()
    {
        if (client != null)
        {
            client.Disconnect(false);
            client.Dispose();
            client = null;
            writeQueue.Clear();
            baCur = null;
            buffCount = 0;
            readBuff.Initialize();
        } 
    }
}
