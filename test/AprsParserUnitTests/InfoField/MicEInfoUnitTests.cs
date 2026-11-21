namespace AprsSharpUnitTests.AprsParser
{
    using System;
    using AprsSharp.AprsParser;
    using Xunit;

    /// <summary>
    /// Tests code in the <see cref="MicEInfo"/> class related to decode of Mic-E packets.
    /// </summary>
    public class MicEInfoUnitTests
    {
        /// <summary>
        /// Verifies decoding a full Mic-E TNC2 packet with current Mic-E type byte (`).
        /// WB7VPC-2 near Chattanooga, TN area.
        /// </summary>
        [Fact]
        public void DecodeFullMicEPacket()
        {
            string raw = "WB7VPC-2>S5PR4Q,W4NAR-2*,WIDE1*,WE4MB-3*,WIDE2*:`q+~lJM>/" + "\"63}=";
            Packet p = new Packet(raw);

            Assert.Equal("WB7VPC-2", p.Sender);
            Assert.IsType<MicEInfo>(p.InfoField);

            MicEInfo mi = (MicEInfo)p.InfoField;

            Assert.NotNull(mi.Position);
            Assert.InRange(mi.Position!.Coordinates.Latitude, 35.0, 36.0);
            Assert.InRange(mi.Position!.Coordinates.Longitude, -86.0, -84.0);

            Assert.NotNull(mi.Speed);
            Assert.NotNull(mi.Course);

            // Symbol table '/' and symbol code '>' (car)
            Assert.Equal('/', mi.Position.SymbolTableIdentifier);
            Assert.Equal('>', mi.Position.SymbolCode);
        }

        /// <summary>
        /// Verifies decoding an old Mic-E format packet (type byte ').
        /// KG4LKY-5 with PEET BROS weather station status text.
        /// </summary>
        [Fact]
        public void DecodeOldMicEPacketWithStatusText()
        {
            string raw = "KG4LKY-5>SWPU0Q,KG4LKY-2*,AC4AG-4*,WE4MB-3*,WIDE3*:'oSl _/]PEET BROS ULTIMETER 2100 TM-D710=";
            Packet p = new Packet(raw);

            Assert.Equal("KG4LKY-5", p.Sender);
            Assert.IsType<MicEInfo>(p.InfoField);

            MicEInfo mi = (MicEInfo)p.InfoField;

            Assert.NotNull(mi.Position);
            Assert.InRange(mi.Position!.Coordinates.Latitude, 37.0, 38.0);
            Assert.InRange(mi.Position!.Coordinates.Longitude, -85.0, -83.0);

            Assert.NotNull(mi.Status);
            Assert.Contains("EET BROS ULTIMETER", mi.Status, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies decoding another Mic-E packet with status text.
        /// KR4DSW-7 with BTECH UV-PRO status.
        /// </summary>
        [Fact]
        public void DecodeAnotherMicEPacketWithStatusText()
        {
            string raw = "KR4DSW-7>SUQV2T,WE4MB-3*,WIDE2-1:`pWOlAL[/BTECH UV-PRO 7.08V" + "\"\"8D}";
            Packet p = new Packet(raw);

            Assert.Equal("KR4DSW-7", p.Sender);
            Assert.IsType<MicEInfo>(p.InfoField);

            MicEInfo mi = (MicEInfo)p.InfoField;

            Assert.NotNull(mi.Position);
            Assert.InRange(mi.Position!.Coordinates.Latitude, 35.0, 36.0);
            Assert.InRange(mi.Position!.Coordinates.Longitude, -87.0, -84.0);

            Assert.NotNull(mi.Status);
        }

        /// <summary>
        /// Verifies that the Packet constructor returns MicEInfo (not UnsupportedInfo)
        /// for a valid Mic-E packet.
        /// </summary>
        [Fact]
        public void PacketConstructorReturnsMicEInfoNotUnsupportedInfo()
        {
            string raw = "WB7VPC-2>S5PR4Q,W4NAR-2*,WIDE1*,WE4MB-3*,WIDE2*:`q+~lJM>/" + "\"63}=";
            Packet p = new Packet(raw);

            Assert.IsType<MicEInfo>(p.InfoField);
            Assert.IsNotType<UnsupportedInfo>(p.InfoField);
        }

        /// <summary>
        /// Verifies that InfoField.FromString with a Mic-E type byte but null destination
        /// returns UnsupportedInfo as a graceful fallback.
        /// </summary>
        [Fact]
        public void FromStringWithMicETypeByteAndNullDestinationReturnsUnsupportedInfo()
        {
            // '`' is the current Mic-E data type byte (not TM-D700)
            string infoField = "`q+~lJM>/" + "\"63}=";
            InfoField result = InfoField.FromString(infoField, null);

            Assert.IsType<UnsupportedInfo>(result);
        }

        /// <summary>
        /// Verifies that InfoField.FromString with a Mic-E type byte and valid destination
        /// returns MicEInfo.
        /// </summary>
        [Fact]
        public void FromStringWithMicETypeByteAndValidDestinationReturnsMicEInfo()
        {
            string infoField = "`q+~lJM>/" + "\"63}=";
            InfoField result = InfoField.FromString(infoField, "S5PR4Q");

            Assert.IsType<MicEInfo>(result);
        }

        /// <summary>
        /// Verifies that a Mic-E info field that is too short (less than 9 characters)
        /// does not throw and returns a MicEInfo with null properties.
        /// </summary>
        [Fact]
        public void InfoFieldTooShortDoesNotThrow()
        {
            // 8 characters: type byte + 7 chars (need at least 9 total)
            string shortInfo = "`q+~lJM>";
            MicEInfo mi = new MicEInfo(shortInfo, "S5PR4Q");

            Assert.Null(mi.Position);
            Assert.Null(mi.Speed);
            Assert.Null(mi.Course);
            Assert.Null(mi.Status);
        }

        /// <summary>
        /// Verifies real-world Mic-E packets whose info fields carry the non-printable bytes
        /// the Mic-E encoding legitimately uses (0x1C-0x1F and 0x7F, i.e. a value + 28).
        /// These previously failed the TNC2 text check and decoded as AX.25 garbage.
        /// </summary>
        /// <param name="raw">The raw TNC2 packet.</param>
        /// <param name="sender">The expected sender.</param>
        /// <param name="latitude">The expected latitude.</param>
        /// <param name="longitude">The expected longitude.</param>
        /// <param name="speed">The expected speed in knots.</param>
        /// <param name="course">The expected course in degrees.</param>
        /// <param name="symbolCode">The expected symbol code.</param>
        [Theory]
        [InlineData("KR4DSW-7>SUQV3Q,KA4J-3*,WE4MB-3*,WIDE2*:`pWRl*\u001c[/BTECH UV-PRO 7.10V\"6m}", "KR4DSW-7", 35.2718, -84.9923, 1, 0, '[')]
        [InlineData("KG4LKY-5>SWPU0Q,KG4LKY-2*,AC4AG-4*,WE4MB-3*,WIDE3*:'oS\u001cl \u001c_/]PEET BROS ULTIMETER 2100 TM-D710=\r", "KG4LKY-5", 37.0835, -83.9167, 0, 0, '_')]
        [InlineData("W2JKT-10>SU1X0Y,YORKSC*,KN6RO-13*,WE4MB-3*,WIDE2*:`m<)l!\u007f>/]\"6u}146.610MHz=\r", "W2JKT-10", 35.3015, -81.5355, 0, 199, '>')]
        public void DecodeMicEPacketWithControlBytes(string raw, string sender, double latitude, double longitude, int speed, int course, char symbolCode)
        {
            Packet p = new Packet(raw);

            Assert.Equal(sender, p.Sender);
            MicEInfo mi = Assert.IsType<MicEInfo>(p.InfoField);
            Assert.NotNull(mi.Position);
            Assert.Equal(latitude, mi.Position!.Coordinates.Latitude, 3);
            Assert.Equal(longitude, mi.Position.Coordinates.Longitude, 3);
            Assert.Equal(speed, mi.Speed);
            Assert.Equal(course, mi.Course);
            Assert.Equal(symbolCode, mi.Position.SymbolCode);
        }
    }
}
