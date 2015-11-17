using System;
using DdpClient;
using DdpClient.EJson;
using DdpClient.Models;
using DdpClient.Models.Client;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DDPClient.Tests
{
    [TestFixture]
    public class DdpConnectionTests
    {
        [SetUp]
        public void Setup()
        {
            _methodId = "ARandomId";
            _mock = new Mock<IDdpWebSocket>();
            _connection = new DdpConnection(_mock.Object)
            {
                IdGenerator = () => _methodId
            };
        }

        private Mock<IDdpWebSocket> _mock;
        private DdpConnection _connection;
        private string _methodId;

        [Test]
        public void ShouldHandleConnectSuccess()
        {
            _mock.Setup(websocket => websocket.Connect())
                .Callback(() => _mock.Raise(webSocket => webSocket.OnOpen += null, null, EventArgs.Empty));

            bool wasRaised = false;
            EventHandler<EventArgs> handler = null;
            handler = delegate (object sender, EventArgs e)
            {
                wasRaised = true;
            };
            _connection.Open += handler;

            _connection.Connect();

            Assert.IsTrue(wasRaised);
            _mock.Verify(webSocket => webSocket.Connect());
            _mock.Verify(webSocket => webSocket.SendJson(It.IsAny<ConnectModel>()));
        }

        [Test]
        public void ShouldHandlePingFromServerWithId()
        {
            string id = "SomeID";
            PingModel ping = new PingModel
            {
                Id = id
            };

            bool wasRaised = false;
            EventHandler<PingModel> handler = null;
            handler = delegate(object sender, PingModel pingMsg)
            {
                wasRaised = true;
                Assert.AreEqual(id, pingMsg.Id);
                _connection.Ping -= handler;
            };
            _connection.Ping += handler;

            _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("ping", JsonConvert.SerializeObject(ping)));
            _mock.Verify(socket => socket.SendJson(It.Is<PongModel>(pong => pong.Id == id)));

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldHandlePingFromServerWithoutId()
        {
            PingModel ping = new PingModel();

            bool wasRaised = false;
            EventHandler<PingModel> handler = null;
            handler = delegate(object sender, PingModel pingMsg)
            {
                wasRaised = true;
                Assert.IsNull(pingMsg.Id);
                _connection.Ping -= handler;
            };
            _connection.Ping += handler;

            _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("ping", JsonConvert.SerializeObject(ping)));
            _mock.Verify(socket => socket.SendJson(It.Is<PongModel>(pong => pong.Id == null)));

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldHandleMethod()
        {
            
        }

        [Test]
        public void ShouldLoginWithUsernameSuccess()
        {
            MethodResponse response = new MethodResponse
            {
                Id = _methodId,
                Error = null,
                Result = new JObject(new JProperty("tokenExpires", JToken.FromObject(new DdpDate())), new JProperty("token", "SomeTokenId"))
            };

            string username = "TestUser";
            string password = "SecretPassword";

            _mock.Setup(websocket => websocket.SendJson(It.IsAny<MethodModel>()))
                .Callback(() => _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("result", JsonConvert.SerializeObject(response))));

            bool wasRaised = false;
            EventHandler<LoginResponse> handler = null;
            handler = delegate (object sender, LoginResponse loginResponse)
            {
                wasRaised = true;
                Assert.IsFalse(loginResponse.HasError());
                Assert.IsNotNull(loginResponse.Token);
                Assert.IsNotNull(loginResponse.TokenExpires);
                _connection.Login -= handler;
            };
            _connection.Login += handler;

            _connection.LoginWithUsername(username, password);

            _mock.Verify(websocket => websocket.SendJson(It.Is<MethodModel>(method =>
                method.Method == "login" &&
                method.Params.Length == 1 &&
                method.Params[0] is BasicLoginModel<UsernameUser>)));

        }

        [Test]
        public void ShouldPingWithId()
        {
            string id = "SomeId";
            _connection.PingServer(id);

            _mock.Verify(websocket => websocket.SendJson(It.Is<PingModel>(ping => ping.Id == id)));
        }

        [Test]
        public void ShouldPingWithoutId()
        {
            _connection.PingServer();

            _mock.Verify(websocket => websocket.SendJson(It.Is<PingModel>(ping => ping.Id == null)));
        }
    }
}