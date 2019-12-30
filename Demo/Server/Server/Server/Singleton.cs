using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Server
{
    public class Singleton<T> where T : new()
    {
        private static T instance;
        private static readonly object lockObject = new object();
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new T();
                        }
                    }
                }
                return instance;
            }
        }
    }
}
