using System;
using DdpClient;
using DdpClient.EJson;
using DdpClient.Models;
using DdpClient.Models.Client;
using DdpClient.Models.Server;
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
            _mock = new Mock<WebSocketAdapterBase>();
            _connection = new DdpConnection(_mock.Object);
        }

        private class TestClass : DdpDocument
        {
            [JsonProperty("data")]
            public int Data { get; set; }
        }

        private Mock<WebSocketAdapterBase> _mock;
        private DdpConnection _connection;

        [Test]
        public void ShouldHandleConnectSuccess()
        {
            _mock.Setup(websocket => websocket.Connect(It.IsAny<string>())).Callback(() => _mock.Raise(webSocket => webSocket.Opened += null, null, EventArgs.Empty));

            bool wasRaised = false;
            EventHandler<EventArgs> handler = null;
            handler = (sender, args) =>
            {
                wasRaised = true;
                _connection.Open -= handler;
            };
            _connection.Open += handler;

            _connection.Connect("");

            Assert.IsTrue(wasRaised);
            _mock.Verify(webSocket => webSocket.Connect(It.IsAny<string>()));
            _mock.Verify(webSocket => webSocket.SendJson(It.IsAny<ConnectModel>()));
        }

        [Test]
        public void ShouldHandleDdpConnectFailed()
        {
            string version = "2";

            _mock.Setup(websocket => websocket.SendJson(It.IsAny<ConnectModel>()))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("failed", "{\"msg\":\"failed\", \"version\":\"" + version + "\"}")));

            bool wasRaised = false;
            EventHandler<ConnectResponse> handler = null;
            handler = (sender, response) =>
            {
                wasRaised = true;
                Assert.IsNull(response.Session);
                Assert.AreEqual(version, response.Failed.Version);
                _connection.Connected -= handler;
            };
            _connection.Connected += handler;

            _mock.Raise(webSocket => webSocket.Opened += null, null, EventArgs.Empty);

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldHandleDdpConnectSuccess()
        {
            string session = "SomeSession";

            _mock.Setup(websocket => websocket.SendJson(It.IsAny<ConnectModel>()))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("connected", "{\"msg\":\"connected\", \"session\":\"" + session + "\"}")));

            bool wasRaised = false;
            EventHandler<ConnectResponse> handler = null;
            handler = (sender, response) =>
            {
                wasRaised = true;
                Assert.IsNull(response.Failed);
                Assert.AreEqual(session, response.Session);
                _connection.Connected -= handler;
            };
            _connection.Connected += handler;

            _mock.Raise(webSocket => webSocket.Opened += null, null, EventArgs.Empty);

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldHandleMethod()
        {
            string id = "ShouldHandleMethod";
            string methodName = "MethodName";
            int parameter = 5;
            _connection.IdGenerator = () => id;

            _connection.Call(methodName, parameter);

            _mock.Verify(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)));
        }

        [Test]
        public void ShouldHandleMethodDynamic()
        {
            string id = "ShouldHandleMethodDynamic";
            string methodName = "MethodName";
            int parameter = 5;
            _connection.IdGenerator = () => id;

            MethodResponse response = new MethodResponse
            {
                Id = id,
                Error = null,
                Result = new JObject(new JProperty("someResult", 10))
            };

            _mock.Setup(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("result", JsonConvert.SerializeObject(response))));

            bool wasCalled = false;
            Action<MethodResponse> callback = mResponse =>
            {
                wasCalled = true;
                Assert.IsNull(mResponse.Error);
                Assert.IsNotNull(mResponse.Result);
            };

            _connection.Call(methodName, callback, parameter);

            _mock.Verify(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)));
            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void ShouldHandleMethodFixedObject()
        {
            const string id = "ShouldHandleMethodFixedObject";
            const string methodName = "MethodName";
            const int parameter = 5;
            const int result = 10;

            _connection.IdGenerator = () => id;

            string mockResult = "{\"msg\":\"result\",\"id\":\"" + id + "\",\"result\":{\"data\": 10}}";

            _mock.Setup(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("result", mockResult)));

            bool wasCalled = false;
            Action<DetailedError, TestClass> callback = (error, mResponse) =>
            {
                wasCalled = true;
                Assert.IsNull(error);
                Assert.AreEqual(result, mResponse.Data);
            };

            _connection.Call(methodName, callback, parameter);

            _mock.Verify(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)));
            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void ShouldHandleMethodFixedValue()
        {
            const string id = "ShouldHandleMethodFixedValue";
            const string methodName = "MethodName";
            const int parameter = 5;
            const int result = 10;

            _connection.IdGenerator = () => id;

            string mockResult = "{\"msg\":\"result\",\"id\":\"" + id + "\",\"result\": 10}";

            _mock.Setup(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("result", mockResult)));

            bool wasCalled = false;
            Action<DetailedError, int> callback = (error, mResponse) =>
            {
                wasCalled = true;
                Assert.IsNull(error);
                Assert.AreEqual(result, mResponse);
            };

            _connection.Call(methodName, callback, parameter);

            _mock.Verify(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)));
            Assert.IsTrue(wasCalled);
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
        public void ShouldHandlePongFromServerWithId()
        {
            string id = "ShouldHandlePongFromServerWithId";

            bool wasRaised = false;
            EventHandler<PongModel> handler = null;
            handler = delegate(object sender, PongModel pong)
            {
                wasRaised = true;
                Assert.AreEqual(id, pong.Id);
                _connection.Pong -= handler;
            };
            _connection.Pong += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("pong", "{\"msg\": \"pong\", \"id\": \"" + id + "\"}"));

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldHandlePongFromServerWithoutId()
        {
            bool wasRaised = false;
            EventHandler<PongModel> handler = null;
            handler = delegate(object sender, PongModel pong)
            {
                wasRaised = true;
                Assert.IsNull(pong.Id);
                _connection.Pong -= handler;
            };
            _connection.Pong += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("pong", "{\"msg\": \"pong\"}"));

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldLoginWithEmailSuccess()
        {
            const string methodId = "SomeRandomId";
            _connection.IdGenerator = () => methodId;

            MethodResponse response = new MethodResponse
            {
                Id = methodId,
                Error = null,
                Result = new JObject(new JProperty("tokenExpires", JToken.FromObject(new DdpDate())), new JProperty("token", "SomeTokenId"))
            };
            _mock.Setup(websocket => websocket.SendJson(It.IsAny<MethodModel>()))
                .Callback(() => _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("result", JsonConvert.SerializeObject(response))));

            string email = "some@email.de";
            string password = "SecretPassword";

            bool wasRaised = false;
            EventHandler<LoginResponse> handler = null;
            handler = delegate(object sender, LoginResponse loginResponse)
            {
                wasRaised = true;
                Assert.IsFalse(loginResponse.HasError());
                Assert.IsNotNull(loginResponse.Token);
                Assert.IsNotNull(loginResponse.TokenExpires);
                _connection.Login -= handler;
            };
            _connection.Login += handler;

            _connection.LoginWithEmail(email, password);

            _mock.Verify(websocket => websocket.SendJson(It.Is<MethodModel>(method =>
                method.Id == methodId &&
                method.Method == "login" &&
                method.Params.Length == 1 &&
                method.Params[0] is BasicLoginModel<EmailUser>)));
            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldLoginWithTokenSuccess()
        {
            String methodId = "SomeRandomId";
            _connection.IdGenerator = () => methodId;

            MethodResponse response = new MethodResponse
            {
                Id = methodId,
                Error = null,
                Result = new JObject(new JProperty("tokenExpires", JToken.FromObject(new DdpDate())), new JProperty("token", "SomeTokenId"))
            };
            _mock.Setup(websocket => websocket.SendJson(It.IsAny<MethodModel>()))
                .Callback(() => _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("result", JsonConvert.SerializeObject(response))));

            string token = "SomeRandomToken";

            bool wasRaised = false;
            EventHandler<LoginResponse> handler = null;
            handler = delegate(object sender, LoginResponse loginResponse)
            {
                wasRaised = true;
                Assert.IsFalse(loginResponse.HasError());
                Assert.IsNotNull(loginResponse.Token);
                Assert.IsNotNull(loginResponse.TokenExpires);
                _connection.Login -= handler;
            };
            _connection.Login += handler;

            _connection.LoginWithToken(token);

            _mock.Verify(websocket => websocket.SendJson(It.Is<MethodModel>(method =>
                method.Id == methodId &&
                method.Method == "login" &&
                method.Params.Length == 1 &&
                method.Params[0] is BasicTokenModel)));
            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldLoginWithUsernameSuccess()
        {
            String methodId = "SomeRandomId";
            _connection.IdGenerator = () => methodId;

            MethodResponse response = new MethodResponse
            {
                Id = methodId,
                Error = null,
                Result = new JObject(new JProperty("tokenExpires", JToken.FromObject(new DdpDate())), new JProperty("token", "SomeTokenId"))
            };
            _mock.Setup(websocket => websocket.SendJson(It.IsAny<MethodModel>()))
                .Callback(() => _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("result", JsonConvert.SerializeObject(response))));

            string username = "TestUser";
            string password = "SecretPassword";

            bool wasRaised = false;
            EventHandler<LoginResponse> handler = null;
            handler = delegate(object sender, LoginResponse loginResponse)
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
                method.Id == methodId &&
                method.Method == "login" &&
                method.Params.Length == 1 &&
                method.Params[0] is BasicLoginModel<UsernameUser>)));
            Assert.IsTrue(wasRaised);
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

        [Test]
        public void ShouldSubscriberHandleAdded()
        {
            string id = "SomeRandomId";
            int data = 5;
            string collection = "tasks";

            TestClass res = new TestClass
            {
                Id = id,
                Data = data
            };
            string mockResult = "{\"msg\":\"added\",\"id\":\"" + id + "\",\"collection\":\"" + collection + "\",\"fields\": " + JsonConvert.SerializeObject(res) + "}";

            DdpSubscriber<TestClass> ddpSubscriber = _connection.GetSubscriber<TestClass>(collection);

            bool wasRaised = false;
            EventHandler<SubAddedModel<TestClass>> handler = null;
            handler = delegate (object sender, SubAddedModel<TestClass> added)
            {
                wasRaised = true;
                Assert.AreEqual(id, added.Id);
                Assert.AreEqual(id, added.Object.Id);
                Assert.AreEqual(data, added.Object.Data);
                ddpSubscriber.Added -= handler;
            };
            ddpSubscriber.Added += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("added", mockResult));

            Assert.IsTrue(wasRaised);
            ddpSubscriber.Dispose();
        }

        [Test]
        public void ShouldSubscriberHandleChanged()
        {
            string id = "SomeRandomId";
            int data = 5;
            string collection = "tasks";

            TestClass res = new TestClass
            {
                Id = id,
                Data = data
            };
            string mockResult = "{\"msg\":\"changed\",\"id\":\"" + id + "\",\"collection\":\"" + collection + "\",\"fields\": " + JsonConvert.SerializeObject(res) + "}";

            DdpSubscriber<TestClass> ddpSubscriber = _connection.GetSubscriber<TestClass>(collection);

            bool wasRaised = false;
            EventHandler<SubChangedModel<TestClass>> handler = null;
            handler = delegate (object sender, SubChangedModel<TestClass> changed)
            {
                wasRaised = true;
                Assert.AreEqual(id, changed.Id);
                Assert.AreEqual(id, changed.Object.Id);
                Assert.AreEqual(data, changed.Object.Data);
                ddpSubscriber.Changed -= handler;
            };
            ddpSubscriber.Changed += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("changed", mockResult));

            Assert.IsTrue(wasRaised);
            ddpSubscriber.Dispose();
        }

        [Test]
        public void ShouldSubscriberHandleRemoved()
        {
            string id = "SomeRandomId";
            string collection = "tasks";

            string mockResult = "{\"msg\":\"removed\",\"id\":\"" + id + "\",\"collection\":\"" + collection + "\"}";

            DdpSubscriber<TestClass> ddpSubscriber = _connection.GetSubscriber<TestClass>(collection);

            bool wasRaised = false;
            EventHandler<SubRemovedModel> handler = null;
            handler = delegate (object sender, SubRemovedModel changed)
            {
                wasRaised = true;
                Assert.AreEqual(id, changed.Id);
                ddpSubscriber.Removed -= handler;
            };
            ddpSubscriber.Removed += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("removed", mockResult));

            Assert.IsTrue(wasRaised);
            ddpSubscriber.Dispose();
        }
    }
}