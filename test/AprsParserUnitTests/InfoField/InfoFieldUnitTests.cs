namespace AprsSharpUnitTests.AprsParser
{
    using AprsSharp.AprsParser;
    using Xunit;

    /// <summary>
    /// Tests code in the <see cref="InfoField"/> class related to encode/decode of the information field.
    /// </summary>
    public class InfoFieldUnitTests
    {
        /// <summary>
        /// Tests GetDataType.
        /// </summary>
        /// <param name="informationField">Input information field to test.</param>
        /// <param name="expectedDataType">Expected data type result.</param>
        [Theory]
        [InlineData("/092345z4903.50N/07201.75W>Test1234", PacketType.PositionWithTimestampNoMessaging)]
        [InlineData(">IO91SX/G", PacketType.Status)]
        [InlineData(";Mt_Toppin*021945z3522.03N/08418.09WETest", PacketType.Object)]
        [InlineData(")146.925!3508.76N/08451.67Wr146.925MHz", PacketType.Item)]
        [InlineData("T#132,179,076,021,066,000,00000000", PacketType.TelemetryData)]
        [InlineData("_03012207c181s000g000t053r000p000P000h83b10210", PacketType.WeatherReport)]
        [InlineData("}EMAIL-2>APJIE4,TCPIP,KG4FZR-3*::KM4ACK   :ack168", PacketType.ThirdPartyTraffic)]
        [InlineData("<IGATE,MSG_CNT=3,LOC_CNT=49", PacketType.StationCapabilities)]
        public void GetDataType(
            string informationField,
            PacketType expectedDataType)
        {
            InfoField infoField = InfoField.FromString(informationField);
            Assert.Equal(expectedDataType, infoField.Type);
        }
    }
}
