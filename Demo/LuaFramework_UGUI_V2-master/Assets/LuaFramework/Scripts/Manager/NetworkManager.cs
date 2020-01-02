using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LuaInterface;

namespace LuaFramework {
    public class NetworkManager : Manager {
        private SocketClient socket;
        static readonly object m_lockObject = new object();

        SocketClient SocketClient {
            get { 
                if (socket == null)
                    socket = new SocketClient();
                return socket;                    
            }
        }

        void Awake() {
            
        }

        public void OnInit() {
            SocketClient.Init();
            CallMethod("Start");
        }

        public void Unload() {
            CallMethod("Unload");
        }

        /// <summary>
        /// 执行Lua方法
        /// </summary>
        public object[] CallMethod(string func, params object[] args) {
            return Util.CallMethod("Network", func, args);
        }
        private void Update()
        {
            while(SocketClient.MsgQueue.Count > 0)
            {
                CallMethod("ReceiveMsg", SocketClient.MsgQueue.Dequeue());
            }
        }

        public void SendMsg(string str)
        {
            SocketClient.Send(str);
        }
        /// <summary>
        /// 析构函数
        /// </summary>
        new void OnDestroy() {
            SocketClient.Close();
            Debug.Log("~NetworkManager was destroy");
        }
    }
}