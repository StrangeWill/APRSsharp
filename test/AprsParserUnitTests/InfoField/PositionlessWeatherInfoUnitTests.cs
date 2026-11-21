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

        /// <summary>
        /// Verifies decoding a Peet Bros Ultimeter "$ULTW" data logger report, including
        /// "----" (not available) fields and unit conversion to the APRS weather units.
        /// </summary>
        [Fact]
        public void DecodeUltimeterDataLoggerReport()
        {
            string encoded = "NE4SC-12>APRS,KN6RO-13*,WE4MB-3*,WIDE2*:$ULTW00A100EB013E1152----000086A0000103190013006E0000006E\r\n";
            Packet p = new Packet(encoded);

            PositionlessWeatherInfo wi = Assert.IsType<PositionlessWeatherInfo>(p.InfoField);
            Assert.Equal(10, wi.WindGust);           // 0x00A1 = 16.1 kph peak
            Assert.Equal(330, wi.WindDirection);     // 0x00EB = 235/256 of a turn
            Assert.Equal(32, wi.Temperature);        // 0x013E = 31.8 degF
            Assert.Null(wi.BarometricPressure);      // "----"
            Assert.Equal(79, wi.Humidity);           // 0x0319 = 79.3 %
            Assert.Equal(0, wi.RainfallSinceMidnight);
            Assert.Equal(7, wi.WindSpeed);           // 0x006E = 11.0 kph average
        }

        /// <summary>
        /// Verifies a negative Ultimeter temperature is decoded as a signed 16-bit value.
        /// </summary>
        [Fact]
        public void DecodeUltimeterNegativeTemperature()
        {
            var wi = Assert.IsType<PositionlessWeatherInfo>(InfoField.FromString("$ULTW00000000FF9C"));
            Assert.Equal(-10, wi.Temperature);       // 0xFF9C = -100 = -10.0 degF
            Assert.Null(wi.Humidity);
        }

        /// <summary>
        /// Verifies a raw NMEA sentence ('$' shares its type byte with the Ultimeter) is not
        /// misinterpreted as weather.
        /// </summary>
        [Fact]
        public void RawNmeaIsNotUltimeterWeather()
        {
            Assert.IsType<UnsupportedInfo>(InfoField.FromString("$GPRMC,091310,A,3242.4489,N,08527.2785,W,000,103,070325,,*1A"));
        }
    }
}
