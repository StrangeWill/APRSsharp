namespace AprsSharp.AprsParser
{
    using System;
    using GeoCoordinatePortable;

    /// <summary>
    /// Represents a Mic-E encoded info field (APRS spec chapter 10).
    /// Position is encoded in the destination address (TOCALL), while
    /// speed, course, and symbol are encoded in the info field body.
    /// </summary>
    public class MicEInfo : InfoField
    {
        /// <summary>
        /// Temporarily holds decoded latitude until longitude is also decoded.
        /// </summary>
        private double? decodedLatitude;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicEInfo"/> class.
        /// </summary>
        /// <param name="encodedInfoField">The encoded info field string (starting with type byte ` or ').</param>
        /// <param name="destination">The destination address (TOCALL) containing the encoded latitude.</param>
        public MicEInfo(string encodedInfoField, string destination)
            : base(encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField))
            {
                throw new ArgumentNullException(nameof(encodedInfoField));
            }

            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentNullException(nameof(destination));
            }

            // Strip any SSID from destination (e.g. "S5PR4Q-2" -> "S5PR4Q")
            string dest = destination.Contains('-') ? destination.Substring(0, destination.IndexOf('-')) : destination;

            if (dest.Length < 6)
            {
                throw new ArgumentException("Destination must be at least 6 characters for Mic-E decoding.", nameof(destination));
            }

            // Info field layout (after the type byte at index 0):
            // [0] = type identifier (` or ' etc.)
            // [1..3] = longitude bytes
            // [4..6] = speed/course bytes
            // [7] = symbol code
            // [8] = symbol table identifier
            // [9..] = status/comment text
            if (encodedInfoField.Length < 9)
            {
                // Not enough data to decode; leave fields null
                return;
            }

            DecodeLatitude(dest);
            DecodeLongitudeAndSymbol(dest, encodedInfoField);
            DecodeSpeedCourse(encodedInfoField);
            DecodeStatus(encodedInfoField);
        }

        /// <summary>
        /// Gets the position decoded from the Mic-E packet.
        /// </summary>
        public Position? Position { get; private set; }

        /// <summary>
        /// Gets the speed in knots, or null if decoding failed.
        /// </summary>
        public int? Speed { get; private set; }

        /// <summary>
        /// Gets the course in degrees, or null if decoding failed.
        /// </summary>
        public int? Course { get; private set; }

        /// <summary>
        /// Gets the status/comment text from the Mic-E info field.
        /// </summary>
        public string? Status { get; private set; }

        /// <summary>
        /// Gets the comment (alias for <see cref="Status"/>).
        /// </summary>
        public string? Comment => Status;

        /// <summary>
        /// Attempts to parse an encoded info field as a <see cref="MicEInfo"/>.
        /// Returns null if the string cannot be parsed.
        /// </summary>
        /// <param name="encodedInfoField">The encoded info field string.</param>
        /// <param name="destination">The destination address (TOCALL) containing the encoded latitude.</param>
        /// <returns>A <see cref="MicEInfo"/> if parsing succeeds; otherwise, null.</returns>
        public static MicEInfo? TryParse(string encodedInfoField, string destination)
        {
            if (string.IsNullOrEmpty(encodedInfoField) || string.IsNullOrEmpty(destination))
            {
                return null;
            }

            try
            {
                return new MicEInfo(encodedInfoField, destination);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override string Encode() =>
            throw new NotSupportedException("MicEInfo encoding is not yet supported.");

        /// <summary>
        /// Decodes a single destination character and returns the latitude digit.
        /// </summary>
        /// <param name="c">A character from the destination field.</param>
        /// <param name="digit">The decoded latitude digit (0-9).</param>
        /// <returns>True if the character is valid for Mic-E latitude encoding.</returns>
        private static bool TryGetLatDigit(char c, out int digit)
        {
            if (c >= '0' && c <= '9')
            {
                digit = c - '0';
                return true;
            }

            if (c >= 'A' && c <= 'J')
            {
                digit = c - 'A';
                return true;
            }

            if (c == 'K' || c == 'L')
            {
                digit = c - 'K';
                return true;
            }

            if (c >= 'P' && c <= 'Y')
            {
                digit = c - 'P';
                return true;
            }

            if (c == 'Z')
            {
                digit = 0;
                return true;
            }

            digit = 0;
            return false;
        }

        /// <summary>
        /// Determines whether a destination character at the given position
        /// indicates North, has a longitude offset of +100, or indicates West.
        /// Characters in the ranges A-K and P-Z are considered "high" for their
        /// respective positional meanings.
        /// </summary>
        private static bool IsHighBit(char c)
        {
            return (c >= 'A' && c <= 'K') || (c >= 'P' && c <= 'Z');
        }

        private void DecodeLatitude(string dest)
        {
            try
            {
                int[] digits = new int[6];
                for (int i = 0; i < 6; i++)
                {
                    if (!TryGetLatDigit(dest[i], out digits[i]))
                    {
                        return;
                    }
                }

                int latDeg = (digits[0] * 10) + digits[1];
                int latMin = (digits[2] * 10) + digits[3];
                int latHundMin = (digits[4] * 10) + digits[5];

                // Position 3 (4th char): N/S. P-Z or A-K = North; 0-9, L = South
                bool isNorth = IsHighBit(dest[3]);

                double lat = latDeg + ((latMin + (latHundMin / 100.0)) / 60.0);
                if (!isNorth)
                {
                    lat = -lat;
                }

                lat = Math.Round(lat, 4);

                // Store latitude temporarily; Position will be created after longitude is decoded
                decodedLatitude = lat;
            }
            catch
            {
                // If anything goes wrong, leave Position null
            }
        }

        private void DecodeLongitudeAndSymbol(string dest, string info)
        {
            try
            {
                if (decodedLatitude == null)
                {
                    return;
                }

                // Position 4 (5th char): longitude offset. P-Z = +100, else 0
                bool lonOffset100 = IsHighBit(dest[4]);

                // Position 5 (6th char): E/W. P-Z = West, else East
                bool isWest = IsHighBit(dest[5]);

                // Longitude bytes are info[1], info[2], info[3]
                int d28 = info[1] - 28;
                int m28 = info[2] - 28;
                int h28 = info[3] - 28;

                int lonDeg = d28;
                if (lonOffset100)
                {
                    lonDeg += 100;
                }

                if (lonDeg >= 180 && lonDeg <= 189)
                {
                    lonDeg -= 80;
                }
                else if (lonDeg >= 190 && lonDeg <= 199)
                {
                    lonDeg -= 190;
                }

                int lonMin = m28;
                if (lonMin >= 60)
                {
                    lonMin -= 60;
                }

                int lonHundMin = h28;

                double lon = lonDeg + ((lonMin + (lonHundMin / 100.0)) / 60.0);
                if (isWest)
                {
                    lon = -lon;
                }

                lon = Math.Round(lon, 4);

                // Symbol: info[7] = symbol code, info[8] = symbol table identifier
                char symbolCode = info[7];
                char symbolTable = info[8];

                Position = new Position(
                    new GeoCoordinate(decodedLatitude.Value, lon),
                    symbolTable,
                    symbolCode);
            }
            catch
            {
                // If anything goes wrong, leave Position null
            }
        }

        private void DecodeSpeedCourse(string info)
        {
            try
            {
                // Speed and course: info[4], info[5], info[6]
                int sp = (info[4] - 28) * 10;
                int dc = info[5] - 28;
                int speedUnits = dc / 10;
                int courseHundreds = dc % 10;

                int speed = sp + speedUnits;
                if (speed >= 800)
                {
                    speed -= 800;
                }

                int se = info[6] - 28;
                int course = (courseHundreds * 100) + se;
                if (course >= 400)
                {
                    course -= 400;
                }

                Speed = speed;
                Course = course;
            }
            catch
            {
                // If anything goes wrong, leave Speed/Course null
            }
        }

        private void DecodeStatus(string info)
        {
            if (info.Length > 9)
            {
                Status = info.Substring(9);
            }
        }
    }
}
