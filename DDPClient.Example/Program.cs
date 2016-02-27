using System;
using DdpClient;
using DdpClient.EJson;
using DdpClient.Models;
using DdpClient.Models.Server;
using Newtonsoft.Json;

namespace DDPClientNet.Example
{
    internal static class Program
    {
        static DdpConnection _client;
        static DdpConnection _client2;

        private static WeakReference _testReference;

        private static void Main(string [] args)
        {
            _client = new DdpConnection(new WebSocketAdapter());
            _client.Retry = true;
            _client.Login += OnLogin;
            _client.Connected += OnConnected;
            _client.Connect("party.johnnycrazydev.de", true);

            _client2 = new DdpConnection(new WebSocketAdapter());
            _client2.Retry = true;
            _client2.Login += OnLogin;
            _client2.Connected += OnConnected;
            _client2.Connect("localhost:3000", false);

            Console.ReadKey();
            _client.Close();
            _client2.Close();
        }

        private static void OnLogin(object sender, LoginResponse loginResponse)
        {
            
        }


        private static void OnConnected(object sender, ConnectResponse connectResponse)
        {
            if(connectResponse.DidFail())
                Console.WriteLine("Connecting Failed" + connectResponse.Failed.Version);
            Console.WriteLine("Connected! Our Session: " + connectResponse.Session);

            _client.Call("createRoom", response =>
            {
                Console.WriteLine(response.Error.Message);
            });
        }
    }
}
