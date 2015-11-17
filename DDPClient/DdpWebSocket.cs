using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace DdpClient
{
    internal class DdpWebSocket : WebSocket, IDdpWebSocket
    {
        public DdpWebSocket(string url, params string[] protocols) : base(url, protocols)
        {
            OnMessage += OnMessageAdv;
        }

        public event EventHandler<DdpMessage> DdpMessage;

        public void SendJson(object body)
        {
            string data = JsonConvert.SerializeObject(body);
            Send(data);
        }

        private void OnMessageAdv(object sender, MessageEventArgs e)
        {
            JObject body = JObject.Parse(e.Data);
            if (body["msg"] == null)
                return;
            string msg = body["msg"].ToObject<string>();
            DdpMessage?.Invoke(this, new DdpMessage(msg, e.Data));
        }
    }
}