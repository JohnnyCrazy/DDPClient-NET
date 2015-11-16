using System;
using System.Collections.Generic;
using DdpClient.EJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DdpClient
{
    public class DdpJsonConverter : JsonConverter
    {
        private readonly Dictionary<Type, Func<JsonReader, object, JsonSerializer, object>> _readTypes;

        public DdpJsonConverter()
        {
            _readTypes = new Dictionary<Type, Func<JsonReader, object, JsonSerializer, object>>
            {
                [typeof (DdpDate)] = (reader, existingValue, serializer) =>
                {
                    JObject ob = JObject.Load(reader);
                    return ob["$date"] == null
                        ? existingValue
                        : new DdpDate
                        {
                            DateTime = Util.MillisecondsToDateTime(ob["$date"].ToObject<long>())
                        };
                },
                [typeof (DdpBinary)] = (reader, existingValue, serializer) =>
                {
                    JObject ob = JObject.Load(reader);
                    return ob["$binary"] == null
                        ? existingValue
                        : new DdpBinary
                        {
                            Data = Util.GetBytesFromBase64(ob["$binary"].ToObject<string>())
                        };
                }
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DdpDate ddpDate = value as DdpDate;
            if (ddpDate != null)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("$date");
                writer.WriteValue(Util.DateTimeToMilliseconds(ddpDate.DateTime));
                writer.WriteEndObject();
                return;
            }
            DdpBinary ddpBinary = value as DdpBinary;
            if (ddpBinary != null)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("$binary");
                writer.WriteValue(Util.GetBase64FromBytes(ddpBinary.Data));
                writer.WriteEndObject();
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return _readTypes.ContainsKey(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return _readTypes[objectType](reader, existingValue, serializer);
        }
    }
}