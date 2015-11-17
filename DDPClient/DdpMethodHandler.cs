using System;
using DdpClient.Models;
using Newtonsoft.Json.Linq;

namespace DdpClient
{
    public class DdpMethodHandler<T>
    {
        private readonly IDdpWebSocket _webSocket;
        private readonly Action<DetailedError, T> _callback;
        public string Id { get; set; }

        public DdpMethodHandler(IDdpWebSocket webSocket, Action<DetailedError, T> callback, string id)
        {
            _webSocket = webSocket;
            _callback = callback;
            _webSocket.DdpMessage += WebSocketOnDdpMessage;

            Id = id;
        }

        private void WebSocketOnDdpMessage(object sender, DdpMessage ddpMessage)
        {
            if (ddpMessage.Msg == "result" && ddpMessage.Body["id"].ToObject<string>() == Id)
            {
                _webSocket.DdpMessage -= WebSocketOnDdpMessage;
                JObject body = ddpMessage.Body;
                if (body["error"] == null)
                    _callback(null, body["result"].ToObject<T>());
                else
                    _callback(body["error"].ToObject<DetailedError>(), default(T));
            }
        }
    }
}