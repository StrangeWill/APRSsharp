namespace AprsSharp.AprsParser
{
    using System;

    /// <summary>
    /// Represents a Third-Party Traffic info field.
    /// Format: }SENDER&gt;TOCALL,PATH:INNER_INFO_FIELD
    /// The inner content is a full TNC2 packet.
    /// </summary>
    public class ThirdPartyTrafficInfo : InfoField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThirdPartyTrafficInfo"/> class.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of a third-party traffic info field.</param>
        public ThirdPartyTrafficInfo(string encodedInfoField)
            : base(encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField) || encodedInfoField[0] != '}')
            {
                throw new ArgumentException("Invalid third-party traffic field", nameof(encodedInfoField));
            }

            var innerRaw = encodedInfoField.Substring(1); // skip '}'
            InnerRaw = innerRaw;

            // Try to parse the inner packet
            try
            {
                InnerPacket = new Packet(innerRaw);
            }
            catch
            {
                // Inner packet unparseable - that's OK, we still have the raw string
            }
        }

        /// <summary>
        /// Gets the raw TNC2 string of the inner packet (without the } prefix).
        /// </summary>
        public string InnerRaw { get; }

        /// <summary>
        /// Gets the parsed inner packet, or null if parsing failed.
        /// </summary>
        public Packet? InnerPacket { get; }

        /// <summary>
        /// Attempts to parse an encoded info field as a <see cref="ThirdPartyTrafficInfo"/>.
        /// Returns null if the string cannot be parsed.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of a <see cref="ThirdPartyTrafficInfo"/>.</param>
        /// <returns>A <see cref="ThirdPartyTrafficInfo"/> if parsing succeeds; otherwise, null.</returns>
        public static ThirdPartyTrafficInfo? TryParse(string encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField) || encodedInfoField[0] != '}')
            {
                return null;
            }

            try
            {
                return new ThirdPartyTrafficInfo(encodedInfoField);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override string Encode() => throw new NotSupportedException($"{nameof(ThirdPartyTrafficInfo)} encoding is not yet supported.");
    }
}
