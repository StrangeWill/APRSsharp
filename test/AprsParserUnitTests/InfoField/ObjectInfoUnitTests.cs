namespace AprsSharpUnitTests.AprsParser
{
    using System;
    using AprsSharp.AprsParser;
    using Xunit;

    /// <summary>
    /// Tests code in the <see cref="ObjectInfo"/> class related to encode/decode of object reports.
    /// </summary>
    public class ObjectInfoUnitTests
    {
        /// <summary>
        /// Verifies decoding a live object from a full TNC2 packet.
        /// </summary>
        [Fact]
        public void DecodeLiveObjectFromPacket()
        {
            string encoded = "KC4OJS-3>APU25N,KQ4HOM-1*,WIDE2-1,qAR,KJ4G-2:;Mt_Toppin*021945z3522.03N/08418.09WEThis Saturday, leaving Hardee's @ 9am";
            Packet p = new Packet(encoded);

            Assert.Equal(PacketType.Object, p.InfoField.Type);

            if (p.InfoField is ObjectInfo oi)
            {
                Assert.Equal("Mt_Toppin", oi.Name);
                Assert.True(oi.IsLive);
                Assert.NotNull(oi.Position);
                Assert.InRange(oi.Position!.Coordinates.Latitude, 35.36, 35.38);
                Assert.InRange(oi.Position!.Coordinates.Longitude, -84.31, -84.30);
                Assert.Contains("Saturday", oi.Comment, StringComparison.Ordinal);
            }
            else
            {
                Assert.IsType<ObjectInfo>(p.InfoField);
            }
        }

        /// <summary>
        /// Verifies decoding a repeater frequency object.
        /// </summary>
        [Fact]
        public void DecodeRepeaterFrequencyObject()
        {
            string infoField = ";147.060  *111111z3526.24N/08434.95Wr147.060MHz T141 -060 KG4FZR";
            ObjectInfo oi = (ObjectInfo)InfoField.FromString(infoField);

            Assert.Equal("147.060", oi.Name);
            Assert.True(oi.IsLive);
            Assert.NotNull(oi.Position);
            Assert.Contains("147.060MHz", oi.Comment, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a killed object (using '_' delimiter) sets IsLive to false.
        /// </summary>
        [Fact]
        public void DecodeKilledObject()
        {
            string infoField = ";Mt_Toppin_021945z3522.03N/08418.09WEKilled object";
            ObjectInfo oi = (ObjectInfo)InfoField.FromString(infoField);

            Assert.Equal("Mt_Toppin", oi.Name);
            Assert.False(oi.IsLive);
        }
    }
}
