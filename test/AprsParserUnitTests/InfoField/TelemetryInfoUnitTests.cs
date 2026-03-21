namespace AprsSharpUnitTests.AprsParser
{
    using AprsSharp.AprsParser;
    using Xunit;

    /// <summary>
    /// Tests code in the <see cref="TelemetryInfo"/> class related to encode/decode of telemetry data.
    /// </summary>
    public class TelemetryInfoUnitTests
    {
        /// <summary>
        /// Verifies decoding a standard telemetry info field.
        /// </summary>
        [Fact]
        public void DecodeStandardTelemetry()
        {
            string infoField = "T#132,179,076,021,066,000,00000000";
            TelemetryInfo ti = (TelemetryInfo)InfoField.FromString(infoField);

            Assert.Equal(132, ti.SequenceNumber);
            Assert.Equal(179, ti.AnalogValues[0]);
            Assert.Equal(76, ti.AnalogValues[1]);
            Assert.Equal(21, ti.AnalogValues[2]);
            Assert.Equal(66, ti.AnalogValues[3]);
            Assert.Equal(0, ti.AnalogValues[4]);
            Assert.NotNull(ti.DigitalBits);
            Assert.All(ti.DigitalBits!, b => Assert.False(b));
        }

        /// <summary>
        /// Verifies decoding max telemetry values.
        /// </summary>
        [Fact]
        public void DecodeMaxTelemetryValues()
        {
            string infoField = "T#999,255,255,255,255,255,11111111";
            TelemetryInfo ti = (TelemetryInfo)InfoField.FromString(infoField);

            Assert.Equal(999, ti.SequenceNumber);
            Assert.All(ti.AnalogValues, v => Assert.Equal(255, v));
            Assert.NotNull(ti.DigitalBits);
            Assert.All(ti.DigitalBits!, b => Assert.True(b));
        }

        /// <summary>
        /// Verifies decoding telemetry from a full TNC2 packet.
        /// </summary>
        [Fact]
        public void DecodeTelemetryFromPacket()
        {
            string encoded = "KN6RO-13>APMI06,AJ4FJ-5*,WE4MB-3*,WIDE2*:T#132,179,076,021,066,000,00000000";
            Packet p = new Packet(encoded);

            Assert.IsType<TelemetryInfo>(p.InfoField);
        }
    }
}
