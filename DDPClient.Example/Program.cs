using System;
using System.Diagnostics;
using DdpClient;
using DdpClient.Models;
using DdpClient.Models.Server;

namespace DDPClientNet.Example
{
    internal static class Program
    {
        private static DdpConnection _client;

        private static void Main(string[] args)
        {
            _client = new DdpConnection();
            _client.Login += OnLogin;
            _client.Closed += OnClose;
            _client.Error += OnError;
            _client.Connected += OnConnected;
            _client.Connect("localhost:3000", false);

            Console.ReadKey();
            _client.Close();
        }

        private static void OnError(object sender, Exception e)
        {
            Debug.WriteLine(e.Message);
            throw e;
        }

        private static void OnClose(object sender, EventArgs eventArgs)
        {
            Console.WriteLine("Closed");
        }

        private static void OnLogin(object sender, LoginResponse loginResponse)
        {
        }


        private static void OnConnected(object sender, ConnectResponse connectResponse)
        {
            if (connectResponse.DidFail())
                Console.WriteLine("Connecting Failed" + connectResponse.Failed.Version);
            Console.WriteLine("Connected! Our Session: " + connectResponse.Session);
        }
    }
}