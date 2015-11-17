using System;
using DdpClient.EJson;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DDPClient.Tests
{
    [TestFixture]
    public class DdpConverterTests
    {
        [Test]
        public void ShouldDeserializeDdpBinary()
        {
            string ddpBinary = "{\"$binary\":\"ICAgICAgIA==\"}";
            DdpBinary expected = new DdpBinary
            {
                Data = new byte[] {0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20}
            };

            DdpBinary result = JsonConvert.DeserializeObject<DdpBinary>(ddpBinary);

            Assert.AreEqual(expected.Data, result.Data);
        }

        [Test]
        public void ShouldDeserializeDdpDate()
        {
            string ddpDate = "{\"$date\":1447770390000}";
            DdpDate expected = new DdpDate
            {
                DateTime = new DateTime(2015, 11, 17, 14, 26, 30)
            };

            DdpDate result = JsonConvert.DeserializeObject<DdpDate>(ddpDate);

            Assert.AreEqual(expected.DateTime, result.DateTime);
        }

        [Test]
        public void ShouldSerializeDdpBinary()
        {
            DdpBinary binary = new DdpBinary
            {
                Data = new byte[] {0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20}
            };
            string expected = "{\"$binary\":\"ICAgICAgIA==\"}";

            string result = JsonConvert.SerializeObject(binary);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ShouldSerializeDdpDate()
        {
            DdpDate date = new DdpDate
            {
                DateTime = new DateTime(2015, 11, 17, 14, 26, 30)
            };
            string expected = "{\"$date\":1447770390000}";

            string result = JsonConvert.SerializeObject(date);

            Assert.AreEqual(expected, result);
        }
    }
}