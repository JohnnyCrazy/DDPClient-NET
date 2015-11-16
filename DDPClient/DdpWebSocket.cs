using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace DdpClient
{
    public class DdpWebSocket : WebSocket
    {
        public DdpWebSocket(string url, params string[] protocols) : base(url, protocols)
        {
            OnMessage += OnMessageAdv;
        }

        internal event EventHandler<DdpMessage> DdpMessage;

        private void OnMessageAdv(object sender, MessageEventArgs e)
        {
            JObject body = JObject.Parse(e.Data);
            if (body["msg"] == null)
                return;
            string msg = body["msg"].ToObject<string>();
            DdpMessage?.Invoke(this, new DdpMessage(msg, e.Data));
        }

        public void SendJson(object body)
        {
            string data = JsonConvert.SerializeObject(body);
            Send(data);
        }
    }
}