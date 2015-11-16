using System;
using DdpClient.Models;
using Newtonsoft.Json.Linq;

namespace DdpClient
{
    public class DdpMethodHandler<T>
    {
        private readonly DdpWebSocket _webSocket;
        private readonly Action<DetailedError, T> _callback;
        public string Id { get; set; }

        public DdpMethodHandler(DdpWebSocket webSocket, Action<DetailedError, T> callback)
        {
            _webSocket = webSocket;
            _callback = callback;
            _webSocket.DdpMessage += WebSocketOnDdpMessage;

            Id = Util.GetRandomId();
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