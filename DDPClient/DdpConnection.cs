using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DdpClient.Models;
using DdpClient.Models.Client;
using DdpClient.Models.Server;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace DdpClient
{
    public class DdpConnection : IDisposable
    {
        private const string Version = "1";

        private Dictionary<string, Action<MethodResponse>> _methods;
        private readonly string[] _supportedProtocols = {"1", "pre1", "pre2"};
        private readonly IDdpWebSocket _webSocket;
        private bool _disposed;

        /// <summary>
        ///     This is raised when the connection is succesful with Meteor (after <see cref="Open" />)
        /// </summary>
        public EventHandler<ConnectResponse> Connected;

        /// <summary>
        ///     This is raised when the connection is closed
        /// </summary>
        public EventHandler<CloseEventArgs> Closed; //TODO Own CloseEventArgs

        /// <summary>
        ///     This is raised when the websocket-library encounters errors
        /// </summary>
        public EventHandler<ErrorEventArgs> Error; //TODO Own ErrorEventArgs

        /// <summary>
        ///     This is raised when the server responeded to a login
        /// </summary>
        public EventHandler<LoginResponse> Login;

        /// <summary>
        ///     This is raised when the connection is opened (before <see cref="Connected" />)
        /// </summary>
        public EventHandler<EventArgs> Open;

        /// <summary>
        ///     This is raised when the server sends a Ping-Msg
        /// </summary>
        public EventHandler<PingModel> Ping;

        /// <summary>
        ///     This is raised when the server sends a Pong-Msg
        /// </summary>
        public EventHandler<PongModel> Pong;

        public DdpConnection(string url, bool ssl = false)
        {
            _webSocket = new DdpWebSocket($"{(ssl ? "wss" : "ws")}://{url}/websocket");

            WebSocketLog = _webSocket.Log;
            WebSocketLog.Output = (data, s) => { }; //Disable console output...

            Initialize();
        }

        public DdpConnection(IDdpWebSocket webSocket)
        {
            _webSocket = webSocket;
            Initialize();
        }

        public string Session { get; set; }

        /// <summary>
        ///     Should the server reconnect after an abnormal close. (Uses Thread.Sleep)
        /// </summary>
        public bool Retry { get; set; }

        public Func<string> IdGenerator { get; set; } 

        public Logger WebSocketLog { get; set; }

        private void Initialize()
        {
            _webSocket.OnOpen += WebSocketOnOpen;
            _webSocket.DdpMessage += WebSocketMessage;
            _webSocket.OnError += WebSocketOnError;
            _webSocket.OnClose += WebSocketOnClose;

            _methods = new Dictionary<string, Action<MethodResponse>>();
            IdGenerator = DdpUtil.GetRandomId;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Connect()
        {
            _webSocket.Connect();
        }

        public void Close()
        {
            Dispose();
        }

        public void PingServer(string id = null)
        {
            _webSocket.SendJson(new PingModel
            {
                Id = id
            });
        }

        public string LoginWithUsername(string username, string password)
        {
            BasicLoginModel<UsernameUser> model = new BasicLoginModel<UsernameUser>
            {
                Password = new PasswordModel
                {
                    Digest = DdpUtil.GetSHA256(password),
                    Algorithm = "sha-256"
                },
                User = new UsernameUser
                {
                    User = username
                }
            };
            return Call("login", HandleLogin, model);
        }

        public string LoginWithEmail(string email, string password)
        {
            BasicLoginModel<EmailUser> model = new BasicLoginModel<EmailUser>
            {
                User = new EmailUser
                {
                    Email = email
                },
                Password = new PasswordModel
                {
                    Digest = DdpUtil.GetSHA256(password),
                    Algorithm = "sha-256"
                }
            };
            return Call("login", HandleLogin, model);
        }

        public string LoginWithToken(string token)
        {
            BasicTokenModel model = new BasicTokenModel
            {
                Resume = token
            };
            return Call("login", HandleLogin, model);
        }

        public void Call(string name, params object[] methodParams)
        {
            MethodModel model = new MethodModel
            {
                Id = IdGenerator(),
                Method = name,
                Params = methodParams
            };
            _webSocket.SendJson(model);
        }

        public string Call(string name, Action<MethodResponse> callback, params object[] methodParams)
        {
            MethodModel model = new MethodModel
            {
                Id = IdGenerator(),
                Method = name,
                Params = methodParams
            };
            _methods[model.Id] = callback;
            _webSocket.SendJson(model);
            return model.Id;
        }

        public DdpMethodHandler<T> Call<T>(string name, Action<DetailedError, T> callback, params object[] methodParams)
        {
            DdpMethodHandler<T> methodHandler = new DdpMethodHandler<T>(_webSocket, callback, IdGenerator());
            MethodModel model = new MethodModel
            {
                Id = methodHandler.Id,
                Method = name,
                Params = methodParams
            };
            _webSocket.SendJson(model);
            return methodHandler;
        }

        public DdpSubscriber<T> GetSubscriber<T>(string collectionName) where T : DdpDocument
        {
            return new DdpSubscriber<T>(_webSocket, collectionName);
        }

        public DdpSubHandler GetSubHandler(string subName, params object[] param)
        {
            return new DdpSubHandler(_webSocket, subName, param);
        }

        private void HandleLogin(MethodResponse response)
        {
            LoginResponse loginResponse = response.Get<LoginResponse>();
            loginResponse.Error = response.Error;
            Login?.Invoke(this, loginResponse);
        }

        private void HandleConnect(bool success, JToken body)
        {
            ConnectResponse response;
            if (success)
                response = body.ToObject<ConnectResponse>();
            else
                response = new ConnectResponse
                {
                    Failed = body.ToObject<FailedModel>()
                };
            Connected?.Invoke(this, response);
        }

        private void HandlePong(PongModel pong)
        {
            Pong?.Invoke(this, pong);
        }

        private void HandlePing(PingModel ping)
        {
            _webSocket.SendJson(new PongModel
            {
                Id = ping.Id
            });
            Ping?.Invoke(this, ping);
        }

        private void HandleMethod(MethodResponse response)
        {
            if (!_methods.ContainsKey(response.Id))
                return;
            Action<MethodResponse> cb = _methods[response.Id];
            _methods.Remove(response.Id);
            cb(response);
        }

        private void WebSocketOnOpen(object sender, EventArgs e)
        {
            _webSocket.SendJson(new ConnectModel
            {
                Session = Session,
                SupportedProtocols = _supportedProtocols,
                Version = Version
            });
            Open?.Invoke(this, e);
        }

        private void WebSocketMessage(object sender, DdpMessage e)
        {
            switch (e.Msg)
            {
                case "connected":
                case "failed":
                    HandleConnect(e.Msg == "connected", e.Get<JToken>());
                    break;
                case "ping":
                    HandlePing(e.Get<PingModel>());
                    break;
                case "pong":
                    HandlePong(e.Get<PongModel>());
                    break;
                case "result":
                    HandleMethod(e.Get<MethodResponse>());
                    break;
            }
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }

        private void WebSocketOnClose(object sender, CloseEventArgs e)
        {
            Closed?.Invoke(this, e);
            if (!e.WasClean && Retry)
            {
                while (!_webSocket.IsAlive)
                {
                    Thread.Sleep(1000);
                    _webSocket.Connect();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //dispose managed resources
                    _webSocket.Close();
                }
            }
            //dispose unmanaged resources
            _disposed = true;
        }
    }
}