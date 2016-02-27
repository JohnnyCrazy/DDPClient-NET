using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DdpClient
{
    public abstract class WebSocketAdapterBase
    {
        public abstract event EventHandler<EventArgs> Opened;
        public abstract event EventHandler<EventArgs> Closed;
        public abstract event EventHandler<Exception> Error;
        protected abstract event EventHandler<string> MessageReceived;

        public event EventHandler<DdpMessage> DdpMessage; 

        public abstract void Connect(string url);

        public abstract void ConnectAsync(string url);

        public abstract void Send(string message);

        public abstract void Close();

        public abstract bool IsAlive();

        protected WebSocketAdapterBase()
        {
            MessageReceived += OnMessageReceived;   
        }

        private void OnMessageReceived(object sender, string e)
        {
            JObject body = JObject.Parse(e);
            if (body["msg"] == null)
                return;
            string msg = body["msg"].ToObject<string>();
            DdpMessage?.Invoke(this, new DdpMessage(msg, e));
        }

        public void SendJson(object body)
        {
            string data = JsonConvert.SerializeObject(body);
            Send(data);
        }
    }
}