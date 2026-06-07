namespace AprsSharp.AprsParser
{
    using System;
    using AprsSharp.AprsParser.Extensions;

    /// <summary>
    /// A representation of an info field on an APRS packet.
    /// </summary>
    public abstract class InfoField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InfoField"/> class.
        /// </summary>
        public InfoField()
        {
            Type = PacketType.Unknown;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfoField"/> class from an encoded string.
        /// </summary>
        /// <param name="encodedInfoField">An encoded InfoField from which to pull the Type.</param>
        public InfoField(string encodedInfoField)
        {
            if (encodedInfoField == null)
            {
                throw new ArgumentNullException(nameof(encodedInfoField));
            }

            Type = GetPacketType(encodedInfoField);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfoField"/> class from a <see cref="PacketType"/>.
        /// </summary>
        /// <param name="type">The <see cref="PacketType"/> of this <see cref="InfoField"/>.</param>
        public InfoField(PacketType type)
        {
            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfoField"/> class from another <see cref="InfoField"/>.
        /// This is the copy constructor.
        /// </summary>
        /// <param name="infoField">An <see cref="InfoField"/> to copy.</param>
        public InfoField(InfoField infoField)
        {
            if (infoField == null)
            {
                throw new ArgumentNullException(nameof(infoField));
            }

            Type = infoField.Type;
        }

        /// <summary>
        /// Gets or sets the <see cref="PacketType"/> of this packet.
        /// </summary>
        public PacketType Type { get; protected set; }

        /// <summary>
        /// Instantiates a type of <see cref="InfoField"/> from the given string.
        /// </summary>
        /// <param name="encodedInfoField">String representation of the APRS info field.</param>
        /// <returns>A class extending <see cref="InfoField"/>.</returns>
        public static InfoField FromString(string encodedInfoField) => FromString(encodedInfoField, null);

        /// <summary>
        /// Instantiates a type of <see cref="InfoField"/> from the given string.
        /// </summary>
        /// <param name="encodedInfoField">String representation of the APRS info field.</param>
        /// <param name="destination">The destination address (TOCALL), required for Mic-E decoding.</param>
        /// <returns>A class extending <see cref="InfoField"/>.</returns>
        public static InfoField FromString(string encodedInfoField, string? destination)
        {
            if (string.IsNullOrEmpty(encodedInfoField))
            {
                return new UnsupportedInfo(encodedInfoField ?? string.Empty);
            }

            PacketType type = GetPacketType(encodedInfoField);

            switch (type)
            {
                case PacketType.PositionWithoutTimestampNoMessaging:
                case PacketType.PositionWithoutTimestampWithMessaging:
                case PacketType.PositionWithTimestampNoMessaging:
                case PacketType.PositionWithTimestampWithMessaging:
                {
                    var positionInfo = PositionInfo.TryParse(encodedInfoField);
                    if (positionInfo == null)
                    {
                        return new UnsupportedInfo(encodedInfoField);
                    }

                    if (positionInfo.Position.IsWeatherSymbol())
                    {
                        return WeatherInfo.TryParse(positionInfo) ?? (InfoField)new UnsupportedInfo(encodedInfoField);
                    }

                    return positionInfo;
                }

                case PacketType.Status:
                    return StatusInfo.TryParse(encodedInfoField) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                case PacketType.MaidenheadGridLocatorBeacon:
                    return MaidenheadBeaconInfo.TryParse(encodedInfoField) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                case PacketType.Message:
                    return MessageInfo.TryParse(encodedInfoField) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                case PacketType.Object:
                    return ObjectInfo.TryParse(encodedInfoField) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                case PacketType.Item:
                    return ItemInfo.TryParse(encodedInfoField) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                case PacketType.StationCapabilities:
                    return StationCapabilitiesInfo.TryParse(encodedInfoField) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                case PacketType.ThirdPartyTraffic:
                    return ThirdPartyTrafficInfo.TryParse(encodedInfoField) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                case PacketType.DoNotUse:
                {
                    // Some misconfigured stations embed valid APRS data after a colon
                    var colonIdx = encodedInfoField.IndexOf(':');
                    if (colonIdx >= 0 && colonIdx + 1 < encodedInfoField.Length)
                    {
                        var innerField = encodedInfoField.Substring(colonIdx + 1);
                        if (innerField.Length > 0)
                        {
                            var innerType = innerField[0].ToPacketType();
                            if (innerType != PacketType.Unknown && innerType != PacketType.DoNotUse)
                            {
                                return FromString(innerField, destination);
                            }
                        }
                    }

                    return new UnsupportedInfo(encodedInfoField);
                }

                case PacketType.TelemetryData:
                    return TelemetryInfo.TryParse(encodedInfoField) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                case PacketType.WeatherReport:
                case PacketType.PeetBrosUIIWeatherStation:
                    return PositionlessWeatherInfo.TryParse(encodedInfoField) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                case PacketType.CurrentMicEData:
                case PacketType.OldMicEData:
                case PacketType.CurrentMicEDataNotTMD700:
                case PacketType.OldMicEDataCurrentTMD700:
                    if (string.IsNullOrEmpty(destination))
                    {
                        return new UnsupportedInfo(encodedInfoField);
                    }

                    return MicEInfo.TryParse(encodedInfoField, destination) ?? (InfoField)new UnsupportedInfo(encodedInfoField);

                default:
                    return new UnsupportedInfo(encodedInfoField);
            }
        }

        /// <summary>
        /// Encodes an APRS info field to a string.
        /// </summary>
        /// <returns>String representation of the packet.</returns>
        public abstract string Encode();

        /// <summary>
        /// Gets the <see cref="PacketType"/> of a string representation of an APRS info field.
        /// </summary>
        /// <param name="encodedInfoField">A string-encoded APRS info field.</param>
        /// <returns><see cref="PacketType"/> of the info field.</returns>
        private static PacketType GetPacketType(string encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField))
            {
                return PacketType.Unknown;
            }

            // TODO Issue #67: This isn't always true.
            // '!' can come up to the 40th position.
            char dataTypeIdentifier = char.ToUpperInvariant(encodedInfoField[0]);

            return dataTypeIdentifier.ToPacketType();
        }
    }
}
