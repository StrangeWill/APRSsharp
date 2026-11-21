namespace AprsSharpUnitTests.AprsParser
{
    using AprsSharp.AprsParser;
    using Xunit;

    /// <summary>
    /// Tests code in the <see cref="ThirdPartyTrafficInfo"/> class related to encode/decode of third-party traffic.
    /// </summary>
    public class ThirdPartyTrafficInfoUnitTests
    {
        /// <summary>
        /// Verifies decoding a third-party traffic info field with a message inner packet.
        /// </summary>
        [Fact]
        public void DecodeThirdPartyTrafficWithMessage()
        {
            string infoField = "}EMAIL-2>APJIE4,TCPIP,KG4FZR-3*::KM4ACK   :ack168";
            ThirdPartyTrafficInfo tpti = (ThirdPartyTrafficInfo)InfoField.FromString(infoField);

            Assert.NotNull(tpti.InnerPacket);
            Assert.IsType<MessageInfo>(tpti.InnerPacket!.InfoField);

            var mi = (MessageInfo)tpti.InnerPacket.InfoField;
            Assert.Equal("KM4ACK", mi.Addressee);
        }

        /// <summary>
        /// Verifies decoding third-party traffic from a full TNC2 packet.
        /// </summary>
        [Fact]
        public void DecodeThirdPartyTrafficFromPacket()
        {
            string encoded = "KG4FZR-3>APDW17,WE4MB-3*,WIDE1*,WIDE2-1:}EMAIL-2>APJIE4,TCPIP,KG4FZR-3*::KM4ACK   :No email address found!{1099";
            Packet p = new Packet(encoded);

            Assert.IsType<ThirdPartyTrafficInfo>(p.InfoField);

            var tpti = (ThirdPartyTrafficInfo)p.InfoField;
            Assert.NotNull(tpti.InnerPacket);
            Assert.Equal("EMAIL-2", tpti.InnerPacket!.Sender);
        }
    }
}
