namespace AprsSharpUnitTests.AprsParser
{
    using AprsSharp.AprsParser;
    using Xunit;

    /// <summary>
    /// Tests code in the <see cref="StationCapabilitiesInfo"/> class related to encode/decode of station capabilities.
    /// </summary>
    public class StationCapabilitiesInfoUnitTests
    {
        /// <summary>
        /// Verifies decoding capabilities with both key-value pairs and standalone keys.
        /// </summary>
        [Fact]
        public void DecodeCapabilitiesWithValues()
        {
            string infoField = "<IGATE,MSG_CNT=3,LOC_CNT=49";
            StationCapabilitiesInfo sci = (StationCapabilitiesInfo)InfoField.FromString(infoField);

            Assert.Equal(3, sci.Capabilities.Count);
            Assert.Null(sci.Capabilities["IGATE"]);
            Assert.Equal("3", sci.Capabilities["MSG_CNT"]);
            Assert.Equal("49", sci.Capabilities["LOC_CNT"]);
        }

        /// <summary>
        /// Verifies decoding capabilities with only standalone keys (no values).
        /// </summary>
        [Fact]
        public void DecodeCapabilitiesWithoutValues()
        {
            string infoField = "<IGATE,DISABLED";
            StationCapabilitiesInfo sci = (StationCapabilitiesInfo)InfoField.FromString(infoField);

            Assert.Equal(2, sci.Capabilities.Count);
            Assert.Null(sci.Capabilities["IGATE"]);
            Assert.Null(sci.Capabilities["DISABLED"]);
        }

        /// <summary>
        /// Verifies decoding station capabilities from a full TNC2 packet.
        /// </summary>
        [Fact]
        public void DecodeCapabilitiesFromPacket()
        {
            string encoded = "KD4YDD-1>APU25N,AJ4FJ-5*,WIDE1*,WE4MB-3*,WIDE2*:<IGATE,MSG_CNT=0,LOC_CNT=54";
            Packet p = new Packet(encoded);

            Assert.IsType<StationCapabilitiesInfo>(p.InfoField);
        }
    }
}
