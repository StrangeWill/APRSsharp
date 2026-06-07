namespace AprsSharpUnitTests.AprsParser;

using System;
using System.Linq;
using System.Text;
using AprsSharp.AprsParser;
using Xunit;

/// <summary>
/// Tests <see cref="Packet"/> code for encoding and decoding AX.25 packet format.
/// </summary>
public class PacketAx25UnitTests
{
    /// <summary>
    /// Tests a full roundtrip encode then decode in AX.25.
    /// </summary>
    [Fact]
    public void RoundTripEncodeDecode()
    {
        var sender = "N0CALL";
        var destination = "N0NE";
        var viaPath = new string[2] { "WIDE2-2", "WIDE1-1" };
        var fullPath = new string[3] { destination, viaPath[0], viaPath[1] };
        var info = new StatusInfo(new Timestamp(DateTime.UtcNow), "Testing 1 2 3!");

        var packet = new Packet(sender, destination, fullPath, info);

        var encoded = packet.EncodeAx25();

        var decodedPacket = new Packet(encoded);

        Assert.Equal(sender, decodedPacket.Sender);
        Assert.Equal(destination, decodedPacket.Destination);
        Assert.Equal(3, decodedPacket.Path.Count);
        Assert.Equal(destination, decodedPacket.Path[0]);
        Assert.Equal(viaPath[0], decodedPacket.Path[1]);
        Assert.Equal(viaPath[1], decodedPacket.Path[2]);

        var si = Assert.IsType<StatusInfo>(decodedPacket.InfoField);
        Assert.NotNull(si.Timestamp);

        Assert.Equal(DateTimeKind.Utc, si.Timestamp.DateTime.Kind);
        Assert.Equal(info.Timestamp!.DateTime.Day, si.Timestamp.DateTime.Day);
        Assert.Equal(info.Timestamp!.DateTime.Hour, si.Timestamp.DateTime.Hour);
        Assert.Equal(info.Timestamp!.DateTime.Minute, si.Timestamp.DateTime.Minute);
        Assert.Equal(info.Comment, si.Comment);
        Assert.Equal(info.Position?.Coordinates, si.Position?.Coordinates);
    }

    /// <summary>
    /// Regression test for misparsing of AX.25 frames whose info field contains a '>'.
    /// A third-party packet ("}SRC&gt;PATH:...") always contains such a '>', which previously
    /// caused the TNC2 text-format heuristic to false-match the raw AX.25 bytes and decode
    /// <see cref="Packet.Sender"/> as binary address garbage instead of the real callsign.
    /// Decoding the raw AX.25 byte frame must yield the outer station as the sender.
    /// </summary>
    [Fact]
    public void DecodeThirdPartyTrafficFromAx25Bytes()
    {
        // The info field carries the '>' that triggered the bug.
        var info = "}KE4QCM-4>APJYC1,TCPIP,K4TUX-10*:=3346.02N/08406.98W-KE4QCM-7 packet 145.59 BBS/Chat/DX";
        var ax25Bytes = BuildAx25Frame("K4TUX-10", "APDW17", new[] { "APDW17", "WIDE1-1" }, info);

        var decoded = new Packet(ax25Bytes);

        Assert.Equal("K4TUX-10", decoded.Sender);
        Assert.Equal("APDW17", decoded.Destination);

        var tpti = Assert.IsType<ThirdPartyTrafficInfo>(decoded.InfoField);
        Assert.NotNull(tpti.InnerPacket);
        Assert.Equal("KE4QCM-4", tpti.InnerPacket!.Sender);
    }

    /// <summary>
    /// Verifies the same third-party packet decodes identically whether it arrives as a
    /// TNC2 string or as a raw AX.25 byte frame. The string (text) path was always correct;
    /// this locks the byte path to parity with it.
    /// </summary>
    [Fact]
    public void ThirdPartyTrafficTnc2AndAx25DecodeIdentically()
    {
        var info = "}KE4QCM-4>APJYC1,TCPIP,K4TUX-10*:=3346.02N/08406.98W-KE4QCM-7 packet 145.59 BBS/Chat/DX";
        var tnc2 = $"K4TUX-10>APDW17,WIDE1-1:{info}";

        var fromString = new Packet(tnc2);
        var fromBytes = new Packet(BuildAx25Frame("K4TUX-10", "APDW17", new[] { "APDW17", "WIDE1-1" }, info));

        Assert.Equal(fromString.Sender, fromBytes.Sender);
        Assert.Equal(fromString.Destination, fromBytes.Destination);

        var stringInfo = Assert.IsType<ThirdPartyTrafficInfo>(fromString.InfoField);
        var bytesInfo = Assert.IsType<ThirdPartyTrafficInfo>(fromBytes.InfoField);
        Assert.Equal(stringInfo.InnerPacket!.Sender, bytesInfo.InnerPacket!.Sender);
    }

    /// <summary>
    /// Verifies a third-party AX.25 byte frame that wraps an inner message decodes correctly,
    /// exposing both the outer sender and the inner addressee.
    /// </summary>
    [Fact]
    public void DecodeThirdPartyMessageFromAx25Bytes()
    {
        var info = "}EMAIL-2>APJIE4,TCPIP,KG4FZR-3*::KM4ACK   :No email address found!{1099";
        var ax25Bytes = BuildAx25Frame("KG4FZR-3", "APDW17", new[] { "APDW17", "WIDE1-1" }, info);

        var decoded = new Packet(ax25Bytes);

        Assert.Equal("KG4FZR-3", decoded.Sender);

        var tpti = Assert.IsType<ThirdPartyTrafficInfo>(decoded.InfoField);
        Assert.Equal("EMAIL-2", tpti.InnerPacket!.Sender);

        var message = Assert.IsType<MessageInfo>(tpti.InnerPacket.InfoField);
        Assert.Equal("KM4ACK", message.Addressee);
    }

    /// <summary>
    /// Regression guard for the general case: an ordinary (non-third-party) AX.25 byte frame
    /// whose info field/comment contains a '>' followed later by a ':' must still decode via
    /// the AX.25 path rather than being misrouted to the TNC2 text heuristic.
    /// </summary>
    [Fact]
    public void DecodeNonThirdPartyAx25WithAngleBracketInComment()
    {
        var sender = "N0CALL-9";
        var destination = "APRS";
        var path = new[] { destination, "WIDE1-1" };
        var info = new StatusInfo(new Timestamp(DateTime.UtcNow), "net>chat: tonight 146.52");

        var ax25Bytes = new Packet(sender, destination, path, info).EncodeAx25();
        var decoded = new Packet(ax25Bytes);

        Assert.Equal(sender, decoded.Sender);
        Assert.Equal(destination, decoded.Destination);

        var status = Assert.IsType<StatusInfo>(decoded.InfoField);
        Assert.Equal(info.Comment, status.Comment);
    }

    /// <summary>
    /// Builds a raw AX.25 byte frame for an arbitrary info-field string. Uses the production
    /// AX.25 encoder to lay down the address/control/PID header, then appends the supplied
    /// info bytes verbatim. This is needed because some info types (e.g.
    /// <see cref="ThirdPartyTrafficInfo"/>) intentionally do not support <c>Encode()</c>.
    /// </summary>
    /// <param name="sender">The sender callsign.</param>
    /// <param name="destination">The destination callsign.</param>
    /// <param name="path">The full path (path[0] is the destination).</param>
    /// <param name="infoText">The raw info-field text to place after the header.</param>
    /// <returns>The raw AX.25 frame bytes.</returns>
    private static byte[] BuildAx25Frame(string sender, string destination, string[] path, string infoText)
    {
        // Header layout produced by EncodeAx25: destination(7) + sender(7) + vias(7*N)
        // + control(1) + PID(1), with the info field immediately after.
        var headerCarrier = new Packet(sender, destination, path, new StatusInfo(new Timestamp(DateTime.UtcNow), "placeholder"));
        var encoded = headerCarrier.EncodeAx25();
        var viaCount = Math.Max(0, path.Length - 1);
        var headerLength = 16 + (viaCount * 7);

        return encoded.Take(headerLength).Concat(Encoding.ASCII.GetBytes(infoText)).ToArray();
    }
}
