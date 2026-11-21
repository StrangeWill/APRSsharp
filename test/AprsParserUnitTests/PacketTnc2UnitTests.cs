namespace AprsSharpUnitTests.AprsParser;

using System;
using AprsSharp.AprsParser;
using Xunit;

/// <summary>
/// Tests <see cref="Packet"/> code for decoding the TNC2 text packet format.
/// </summary>
public class PacketTnc2UnitTests
{
    /// <summary>
    /// Verifies a TNC2 packet whose info field contains DEL (0x7F) bytes is still decoded as
    /// TNC2. Only the header decides TNC2 vs AX.25; the info field may carry any byte.
    /// </summary>
    [Fact]
    public void DecodePositionWithDelBytesInComment()
    {
        string raw = "K4UAN-3>APN383,KM4BJZ-2*,WE4MB-3*,WIDE2*:!3605.01NS08522.30W#PHG5460 W3\u007f2, TNn-N, Servic\u007fng The Upper Cumberland\r";
        Packet p = new Packet(raw);

        Assert.Equal("K4UAN-3", p.Sender);
        Assert.Equal("APN383", p.Destination);
        PositionInfo pi = Assert.IsType<PositionInfo>(p.InfoField);
        Assert.Equal(36.0835, pi.Position.Coordinates.Latitude, 4);
    }

    /// <summary>
    /// Verifies a TNC2 packet whose info field contains an embedded newline is decoded.
    /// </summary>
    [Fact]
    public void DecodeInfoFieldWithEmbeddedNewline()
    {
        string raw = "KM4KMO-10>APWW11,KM4KMO-14*,WIDE1*,WE4MB-3*,WIDE2*,RFONLY:<IGATE,DISABLED\n,DX=1*N4YH(24mi@316?)\n\r";
        Packet p = new Packet(raw);

        Assert.Equal("KM4KMO-10", p.Sender);
        Assert.IsType<StationCapabilitiesInfo>(p.InfoField);
    }

    /// <summary>
    /// Verifies a TNC2 packet with an empty info field decodes to <see cref="UnsupportedInfo"/>
    /// rather than falling through to the AX.25 decoder and throwing.
    /// </summary>
    [Fact]
    public void DecodeEmptyInfoField()
    {
        Packet p = new Packet("W3DEV>N4LMC-10:");

        Assert.Equal("W3DEV", p.Sender);
        Assert.Equal("N4LMC-10", p.Destination);
        Assert.IsType<UnsupportedInfo>(p.InfoField);
    }

    /// <summary>
    /// Verifies a buffer that is neither TNC2 nor long enough to be AX.25 throws an
    /// <see cref="ArgumentException"/> rather than an index-out-of-range error.
    /// </summary>
    [Fact]
    public void TruncatedNonTextFrameThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Packet(")mg\u00117,-4>D%Y\f^f-10:\f,??"));
    }
}
