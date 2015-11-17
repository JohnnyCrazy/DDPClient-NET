using System;
using DdpClient.Models.Client;
using DdpClient.Models.Server;

namespace DdpClient
{
    public class DdpSubHandler : IDisposable
    {
        private readonly IDdpWebSocket _webSocket;

        public EventHandler<NoSubModel> NoSub;
        public EventHandler<EventArgs> Ready;

        public DdpSubHandler(IDdpWebSocket webSocket, string subName, params object[] subParams)
        {
            _webSocket = webSocket;
            _webSocket.DdpMessage += Message;

            Params = subParams;
            Name = subName;
            Id = DdpUtil.GetRandomId();
        }

        public string Id { get; }
        public object[] Params { get; set; }
        public string Name { get; set; }

        public void Dispose()
        {
        }

        public void Sub()
        {
            SubModel subModel = new SubModel
            {
                Id = Id,
                Name = Name,
                Params = Params
            };
            if (!_webSocket.IsAlive)
                throw new InvalidOperationException("The DDP-Connection is not alive anymore");
            _webSocket.SendJson(subModel);
        }

        public void Unsub()
        {
            UnsubModel unsubModel = new UnsubModel
            {
                Id = Id
            };
            if (!_webSocket.IsAlive)
                throw new InvalidOperationException("The DDP-Connection is not alive anymore");
            _webSocket.SendJson(unsubModel);
        }

        private void HandleNoSub(NoSubModel noSub)
        {
            NoSub?.Invoke(this, noSub);
        }

        private void HandleReady()
        {
            Ready?.Invoke(this, EventArgs.Empty);
        }

        private void Message(object sender, DdpMessage e)
        {
            switch (e.Msg)
            {
                case "nosub":
                    HandleNoSub(e.Get<NoSubModel>());
                    break;
                case "ready":
                    SubReadyModel ready = e.Get<SubReadyModel>();
                    if (ready.Subs.Contains(Id))
                        HandleReady();
                    break;
            }
        }
    }
}