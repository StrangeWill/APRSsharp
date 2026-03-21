namespace AprsSharp.AprsParser
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a Station Capabilities info field.
    /// Format: &lt;CAPABILITY1,KEY2=VALUE2,...
    /// </summary>
    public class StationCapabilitiesInfo : InfoField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StationCapabilitiesInfo"/> class.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of a station capabilities info field.</param>
        public StationCapabilitiesInfo(string encodedInfoField)
            : base(encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField))
            {
                throw new ArgumentException("Invalid capabilities field", nameof(encodedInfoField));
            }

            var data = encodedInfoField.Substring(1); // skip '<'
            Capabilities = new Dictionary<string, string?>();

            foreach (var part in data.Split(','))
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx >= 0)
                {
                    Capabilities[trimmed.Substring(0, eqIdx)] = trimmed.Substring(eqIdx + 1);
                }
                else
                {
                    Capabilities[trimmed] = null;
                }
            }
        }

        /// <summary>
        /// Gets the parsed capabilities dictionary.
        /// Keys without values have null as their value.
        /// </summary>
        public Dictionary<string, string?> Capabilities { get; }

        /// <summary>
        /// Attempts to parse an encoded info field as a <see cref="StationCapabilitiesInfo"/>.
        /// Returns null if the string cannot be parsed.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of a <see cref="StationCapabilitiesInfo"/>.</param>
        /// <returns>A <see cref="StationCapabilitiesInfo"/> if parsing succeeds; otherwise, null.</returns>
        public static StationCapabilitiesInfo? TryParse(string encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField))
            {
                return null;
            }

            try
            {
                return new StationCapabilitiesInfo(encodedInfoField);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override string Encode() => throw new NotSupportedException($"{nameof(StationCapabilitiesInfo)} encoding is not yet supported.");
    }
}
