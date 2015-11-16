using System;
using DdpClient;
using DdpClient.EJson;
using DdpClient.Models;
using DdpClient.Models.Server;
using Newtonsoft.Json;

namespace DDPClientNet.Example
{
    internal class Program
    {
        static DdpConnection _client;

        static void Main(string [] args)
        {
            _client = new DdpConnection("localhost:3000");
            _client.Retry = true;
            _client.Connected += OnConnected;
            _client.Connect();

            Console.ReadKey();
            _client.Close();
        }

        private static void OnConnected(object sender, ConnectResponse connectResponse)
        {
            if(connectResponse.DidFail())
                Console.WriteLine("Connecting Failed");
            Console.WriteLine("Connected! Our Session: " + connectResponse.Session);


        }
    }

    internal class Task : DdpDocument
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }

        [JsonProperty("createdAt")]
        public DdpDate CreatedAt { get; set; }
    }
}
