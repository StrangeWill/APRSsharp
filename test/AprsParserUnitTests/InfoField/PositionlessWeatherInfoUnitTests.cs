namespace AprsSharpUnitTests.AprsParser
{
    using AprsSharp.AprsParser;
    using Xunit;

    /// <summary>
    /// Tests code in the <see cref="PositionlessWeatherInfo"/> class related to encode/decode of positionless weather reports.
    /// </summary>
    public class PositionlessWeatherInfoUnitTests
    {
        /// <summary>
        /// Verifies decoding a positionless weather info field.
        /// </summary>
        [Fact]
        public void DecodePositionlessWeather()
        {
            string infoField = "_03012207c181s000g000t053r000p000P000h83b10210tRSW";
            PositionlessWeatherInfo wi = (PositionlessWeatherInfo)InfoField.FromString(infoField);

            Assert.Equal("03012207", wi.TimestampString);
            Assert.Equal(181, wi.WindDirection);
            Assert.Equal(53, wi.Temperature);
            Assert.Equal(83, wi.Humidity);
            Assert.Equal(10210, wi.BarometricPressure);
        }

        /// <summary>
        /// Verifies decoding positionless weather from a full TNC2 packet.
        /// </summary>
        [Fact]
        public void DecodePositionlessWeatherFromPacket()
        {
            string encoded = "AJ4FJ-13>APTW14,KN6RO-13*,WIDE1*,WE4MB-3*,WIDE2-1:_03012207c181s000g000t053r000p000P000h83b10210tRSW";
            Packet p = new Packet(encoded);

            Assert.IsType<PositionlessWeatherInfo>(p.InfoField);
        }

        /// <summary>
        /// Verifies decoding a second positionless weather report.
        /// </summary>
        [Fact]
        public void DecodeSecondPositionlessWeather()
        {
            string infoField = "_03020126c153s000g000t052r000p000P000h61b10220wU2K";
            PositionlessWeatherInfo wi = (PositionlessWeatherInfo)InfoField.FromString(infoField);

            Assert.Equal(153, wi.WindDirection);
            Assert.Equal(52, wi.Temperature);
            Assert.Equal(61, wi.Humidity);
        }
    }
}
