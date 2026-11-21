namespace AprsSharp.AprsParser
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents a positionless weather report info field (_ prefix).
    /// Format: _MMDDHHMMcDDDsSSS gGGGtTTTrRRRpPPPPPPPPhHHbBBBBB[comment].
    /// </summary>
    public class PositionlessWeatherInfo : InfoField
    {
        private const string UltimeterPrefix = "$ULTW";

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionlessWeatherInfo"/> class from an encoded info field.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of a positionless weather report.</param>
        public PositionlessWeatherInfo(string encodedInfoField)
            : base(encodedInfoField)
        {
            if (IsUltimeterDataLogger(encodedInfoField))
            {
                // Peet Bros Ultimeter "$ULTW" data logger format (APRS 1.01 spec chapter 12):
                // a run of 4-hex-digit fields, "----" meaning not available.
                //   [0]  peak wind speed over last 5 min (0.1 kph)
                //   [1]  direction of peak wind (0-255)
                //   [2]  outdoor temperature (0.1 degF, signed)
                //   [3]  long-term rain total (0.01 in)
                //   [4]  barometer (0.1 mbar)
                //   [5..7] barometer delta / correction factor
                //   [8]  outdoor humidity (0.1 %)
                //   [9]  day of year, [10] minute of day
                //   [11] today's rain total (0.01 in)
                //   [12] 5-minute average wind speed (0.1 kph)
                var fields = encodedInfoField.Substring(UltimeterPrefix.Length).TrimEnd('\r', '\n');
                const double kphTenthsToMph = 0.1 * 0.621371;

                WindGust = Scale(UltimeterField(fields, 0), kphTenthsToMph);
                WindDirection = Scale(UltimeterField(fields, 1), 360.0 / 256.0);
                Temperature = Scale(UltimeterField(fields, 2, signed: true), 0.1);
                BarometricPressure = UltimeterField(fields, 4);
                Humidity = Scale(UltimeterField(fields, 8), 0.1);
                RainfallSinceMidnight = UltimeterField(fields, 11);
                WindSpeed = Scale(UltimeterField(fields, 12), kphTenthsToMph);
                return;
            }

            if (string.IsNullOrEmpty(encodedInfoField) || encodedInfoField[0] != '_')
            {
                throw new ArgumentException("Invalid positionless weather info field", nameof(encodedInfoField));
            }

            var data = encodedInfoField.Substring(1);

            // First 8 chars are MMDDHHMM timestamp
            if (data.Length >= 8)
            {
                TimestampString = data.Substring(0, 8);
                data = data.Substring(8);
            }

            // Parse weather fields from the remaining data
            // Same format as position weather: cDDD/SSSgGGGtTTT...
            WindDirection = GetMeasurement(data, 'c', 3) ?? GetMeasurement(data, '^', 3);
            WindSpeed = GetMeasurement(data, '/', 3) ?? GetMeasurement(data, 's', 3);
            WindGust = GetMeasurement(data, 'g', 3);
            Temperature = GetMeasurement(data, 't', 3);
            Rainfall1Hour = GetMeasurement(data, 'r', 3);
            Rainfall24Hour = GetMeasurement(data, 'p', 3);
            RainfallSinceMidnight = GetMeasurement(data, 'P', 3);
            Humidity = GetMeasurement(data, 'h', 2);
            BarometricPressure = GetMeasurement(data, 'b', 5);
            Luminosity = GetMeasurement(data, 'L', 3) ?? GetMeasurement(data, 'l', 3);
            Snow = GetMeasurement(data, 's', 3);

            // Comment is non-weather text at the end
            Comment = GetUserComment(data);
        }

        /// <summary>
        /// Gets the raw timestamp string (MMDDHHMM).
        /// </summary>
        public string? TimestampString { get; }

        /// <summary>
        /// Gets wind direction in degrees.
        /// </summary>
        public int? WindDirection { get; }

        /// <summary>
        /// Gets wind speed 1-minute sustained in miles per hour.
        /// </summary>
        public int? WindSpeed { get; }

        /// <summary>
        /// Gets 5-minute max wind gust in miles per hour.
        /// </summary>
        public int? WindGust { get; }

        /// <summary>
        /// Gets temperature in degrees Fahrenheit.
        /// </summary>
        public int? Temperature { get; }

        /// <summary>
        /// Gets 1-hour rainfall in 100ths of an inch.
        /// </summary>
        public int? Rainfall1Hour { get; }

        /// <summary>
        /// Gets 24-hour rainfall in 100ths of an inch.
        /// </summary>
        public int? Rainfall24Hour { get; }

        /// <summary>
        /// Gets rainfall since midnight in 100ths of an inch.
        /// </summary>
        public int? RainfallSinceMidnight { get; }

        /// <summary>
        /// Gets humidity in percentage.
        /// </summary>
        public int? Humidity { get; }

        /// <summary>
        /// Gets barometric pressure in 10ths of millibars/10ths of hPascal.
        /// </summary>
        public int? BarometricPressure { get; }

        /// <summary>
        /// Gets luminosity in watts per square meter.
        /// </summary>
        public int? Luminosity { get; }

        /// <summary>
        /// Gets snowfall in inches in the last 24 hours.
        /// </summary>
        public int? Snow { get; }

        /// <summary>
        /// Gets user comment within weather data.
        /// </summary>
        public string? Comment { get; }

        /// <summary>
        /// Attempts to parse an encoded info field as a <see cref="PositionlessWeatherInfo"/>.
        /// Returns null if the string cannot be parsed.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of a <see cref="PositionlessWeatherInfo"/>.</param>
        /// <returns>A <see cref="PositionlessWeatherInfo"/> if parsing succeeds; otherwise, null.</returns>
        public static PositionlessWeatherInfo? TryParse(string encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField) || (encodedInfoField[0] != '_' && !IsUltimeterDataLogger(encodedInfoField)))
            {
                return null;
            }

            try
            {
                return new PositionlessWeatherInfo(encodedInfoField);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override string Encode() => throw new NotSupportedException("PositionlessWeatherInfo encoding not yet supported");

        /// <summary>
        /// Determines whether an info field is a Peet Bros Ultimeter "$ULTW" data logger report.
        /// </summary>
        /// <param name="encodedInfoField">The encoded info field.</param>
        /// <returns>True if the info field starts with "$ULTW".</returns>
        internal static bool IsUltimeterDataLogger(string? encodedInfoField) =>
            encodedInfoField != null && encodedInfoField.StartsWith(UltimeterPrefix, StringComparison.Ordinal);

        private static int? UltimeterField(string fields, int index, bool signed = false)
        {
            int offset = index * 4;
            if (offset + 4 > fields.Length)
            {
                return null;
            }

            var hex = fields.Substring(offset, 4);
            if (!int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int value))
            {
                return null;
            }

            return signed ? (short)value : value;
        }

        private static int? Scale(int? value, double factor) =>
            value.HasValue ? (int)Math.Round(value.Value * factor, MidpointRounding.AwayFromZero) : null;

        private static int? GetMeasurement(string data, char key, int length)
        {
            var match = Regex.Match(data, $@"{Regex.Escape(key.ToString())}((\d{{{length}}})|(-\d{{{length - 1}}}))");
            return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val)
                ? val
                : (int?)null;
        }

        private static string? GetUserComment(string data)
        {
            // Strip all known weather measurement patterns from the data
            // What remains is the user comment
            var stripped = data;
            stripped = Regex.Replace(stripped, @"c\d{3}", string.Empty);
            stripped = Regex.Replace(stripped, @"/\d{3}", string.Empty);
            stripped = Regex.Replace(stripped, @"s\d{3}", string.Empty);
            stripped = Regex.Replace(stripped, @"g\d{3}", string.Empty);
            stripped = Regex.Replace(stripped, @"t-?\d{2,3}", string.Empty);
            stripped = Regex.Replace(stripped, @"r\d{3}", string.Empty);
            stripped = Regex.Replace(stripped, @"p\d{3,}", string.Empty);
            stripped = Regex.Replace(stripped, @"P\d{3,}", string.Empty);
            stripped = Regex.Replace(stripped, @"h\d{2}", string.Empty);
            stripped = Regex.Replace(stripped, @"b\d{5}", string.Empty);
            stripped = Regex.Replace(stripped, @"[Ll]\d{3}", string.Empty);
            stripped = Regex.Replace(stripped, @"#\d{3}", string.Empty);
            return string.IsNullOrEmpty(stripped) ? null : stripped;
        }
    }
}
