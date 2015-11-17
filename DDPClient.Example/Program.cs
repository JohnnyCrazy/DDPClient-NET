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
            _client.Login += Login;
            _client.Connected += OnConnected;
            _client.Connect();

            Console.ReadKey();
            _client.Close();
        }

        private static void Login(object sender, LoginResponse loginResponse)
        {

        }

        private static void OnConnected(object sender, ConnectResponse connectResponse)
        {
            if(connectResponse.DidFail())
                Console.WriteLine("Connecting Failed" + connectResponse.Failed.Version);
            Console.WriteLine("Connected! Our Session: " + connectResponse.Session);


        }
    }

    internal class TaskSubscriber : IDdpSubscriber<Task>
    {
        public void Added(SubAddedModel<Task> added)
        {
            
        }

        public void AddedBefore(SubAddedBeforeModel<Task> addedBefore)
        {
            throw new NotImplementedException();
        }

        public void MovedBefore(SubMovedBeforeModel movedBefore)
        {
            throw new NotImplementedException();
        }

        public void Changed(SubChangedModel<Task> changed)
        {
            throw new NotImplementedException();
        }

        public void Removed(SubRemovedModel removed)
        {
            throw new NotImplementedException();
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
