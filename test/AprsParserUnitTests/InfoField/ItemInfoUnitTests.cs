namespace AprsSharpUnitTests.AprsParser
{
    using AprsSharp.AprsParser;
    using Xunit;

    /// <summary>
    /// Tests code in the <see cref="ItemInfo"/> class related to encode/decode of item reports.
    /// </summary>
    public class ItemInfoUnitTests
    {
        /// <summary>
        /// Verifies decoding a live item from a full TNC2 packet.
        /// </summary>
        [Fact]
        public void DecodeLiveItemFromPacket()
        {
            string encoded = "KA4J-3>APWW11,TCPIP*,qAC,T2RDU:)146.925!3508.76N/08451.67Wr146.925MHz T114 -0600 W4GZX";
            Packet p = new Packet(encoded);

            Assert.Equal(PacketType.Item, p.InfoField.Type);

            if (p.InfoField is ItemInfo ii)
            {
                Assert.Equal("146.925", ii.Name);
                Assert.True(ii.IsLive);
                Assert.NotNull(ii.Position);
            }
            else
            {
                Assert.IsType<ItemInfo>(p.InfoField);
            }
        }

        /// <summary>
        /// Verifies that a killed item (using '_' delimiter) sets IsLive to false.
        /// </summary>
        [Fact]
        public void DecodeKilledItem()
        {
            string infoField = ")146.925_3508.76N/08451.67Wr146.925MHz T114 -0600 W4GZX";
            ItemInfo ii = (ItemInfo)InfoField.FromString(infoField);

            Assert.Equal("146.925", ii.Name);
            Assert.False(ii.IsLive);
        }

        /// <summary>
        /// Verifies that a compressed position item parses without throwing.
        /// </summary>
        [Fact]
        public void DecodeCompressedPositionItem()
        {
            string encoded = "N0CALL>WIDE1-1:)147.045IA!/9Qi+6Xcqr   /A=000200N0BKB";
            Packet p = new Packet(encoded);

            Assert.IsType<ItemInfo>(p.InfoField);
        }
    }
}
