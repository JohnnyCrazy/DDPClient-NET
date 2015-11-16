using Newtonsoft.Json;

namespace DdpClient
{
    public class DdpDocument
    {
        [JsonProperty]
        public string Id { get; set; }
    }
}