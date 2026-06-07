namespace AprsSharp.AprsParser
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Represents a telemetry data info field (T# prefix).
    /// Format: T#SSS,AAA,AAA,AAA,AAA,AAA,BBBBBBBB[,comment].
    /// </summary>
    public class TelemetryInfo : InfoField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryInfo"/> class from an encoded info field.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of telemetry data.</param>
        public TelemetryInfo(string encodedInfoField)
            : base(encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField))
            {
                throw new ArgumentException("Invalid telemetry info field", nameof(encodedInfoField));
            }

            // Skip 'T#' prefix
            var data = encodedInfoField;
            if (data.StartsWith("T#", StringComparison.Ordinal))
            {
                data = data.Substring(2);
            }
            else if (data.StartsWith("T", StringComparison.Ordinal))
            {
                data = data.Substring(1);
            }

            var parts = data.Split(',');

            if (parts.Length >= 1 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var seq))
            {
                SequenceNumber = seq;
            }

            AnalogValues = new int?[5];
            for (int i = 0; i < 5 && i + 1 < parts.Length; i++)
            {
                if (int.TryParse(parts[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                {
                    AnalogValues[i] = val;
                }
            }

            if (parts.Length >= 7)
            {
                var bitsStr = parts[6];
                DigitalBits = new bool[8];
                for (int i = 0; i < 8 && i < bitsStr.Length; i++)
                {
                    DigitalBits[i] = bitsStr[i] == '1';
                }

                // Any remaining parts after the digital bits are comment
                if (parts.Length > 7)
                {
                    Comment = string.Join(",", parts, 7, parts.Length - 7);
                }
            }
        }

        /// <summary>
        /// Gets the telemetry sequence number (001-999).
        /// </summary>
        public int? SequenceNumber { get; }

        /// <summary>
        /// Gets the five analog telemetry values.
        /// </summary>
        public int?[] AnalogValues { get; } = new int?[5];

        /// <summary>
        /// Gets the eight digital bits.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:ClosingSquareBracketsMustBeSpacedCorrectly", Justification = "Nullable array syntax.")]
        public bool[]? DigitalBits { get; }

        /// <summary>
        /// Gets an optional comment appended after the telemetry data.
        /// </summary>
        public string? Comment { get; }

        /// <summary>
        /// Attempts to parse an encoded info field as a <see cref="TelemetryInfo"/>.
        /// Returns null if the string cannot be parsed.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of a <see cref="TelemetryInfo"/>.</param>
        /// <returns>A <see cref="TelemetryInfo"/> if parsing succeeds; otherwise, null.</returns>
        public static TelemetryInfo? TryParse(string encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField))
            {
                return null;
            }

            try
            {
                return new TelemetryInfo(encodedInfoField);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override string Encode() => throw new NotSupportedException("TelemetryInfo encoding not yet supported");
    }
}
