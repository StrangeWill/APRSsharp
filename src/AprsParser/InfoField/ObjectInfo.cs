namespace AprsSharp.AprsParser
{
    using System;
    using AprsSharp.AprsParser.Extensions;

    /// <summary>
    /// Represents an info field for an APRS Object report.
    /// Format: ;NAME_____*DDHHMMzPOSITION_DATAComment
    /// where NAME is 9 chars (space-padded), * = live / _ = killed,
    /// followed by a 7-char timestamp, position data, and optional comment.
    /// </summary>
    public class ObjectInfo : InfoField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectInfo"/> class.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of an <see cref="ObjectInfo"/>.</param>
        public ObjectInfo(string encodedInfoField)
            : base(encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField))
            {
                throw new ArgumentException("Invalid object info field", nameof(encodedInfoField));
            }

            // Skip the ; prefix
            var data = encodedInfoField.Substring(1);

            if (data.Length < 10)
            {
                throw new ArgumentException("Object info field too short", nameof(encodedInfoField));
            }

            // Object name is exactly 9 characters
            Name = data.Substring(0, 9).TrimEnd();

            // Live/killed indicator
            char liveKilled = data[9];
            if (liveKilled != '*' && liveKilled != '_')
            {
                throw new ArgumentException($"Expected '*' or '_' at position 10, found '{liveKilled}'", nameof(encodedInfoField));
            }

            IsLive = liveKilled == '*';

            var remainder = data.Substring(10);

            // Next 7 chars are timestamp
            if (remainder.Length >= 7)
            {
                Timestamp = new Timestamp(remainder.Substring(0, 7));
                remainder = remainder.Substring(7);
            }
            else
            {
                throw new ArgumentException("Object info field missing timestamp", nameof(encodedInfoField));
            }

            // Remaining is position + comment
            DecodePositionAndComment(remainder);
        }

        /// <summary>
        /// Gets the object name (up to 9 characters, trimmed).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the object is live (true) or killed (false).
        /// </summary>
        public bool IsLive { get; }

        /// <summary>
        /// Gets the timestamp of the object report.
        /// </summary>
        public Timestamp? Timestamp { get; }

        /// <summary>
        /// Gets the position of the object.
        /// </summary>
        public Position? Position { get; private set; }

        /// <summary>
        /// Gets the comment from the object report.
        /// </summary>
        public string? Comment { get; private set; }

        /// <summary>
        /// Attempts to parse an encoded info field as an <see cref="ObjectInfo"/>.
        /// Returns null if the string cannot be parsed.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of an <see cref="ObjectInfo"/>.</param>
        /// <returns>An <see cref="ObjectInfo"/> if parsing succeeds; otherwise, null.</returns>
        public static ObjectInfo? TryParse(string encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField) || encodedInfoField.Length < 11)
            {
                return null;
            }

            try
            {
                return new ObjectInfo(encodedInfoField);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override string Encode() => throw new NotSupportedException($"{nameof(ObjectInfo)} encoding is not yet supported.");

        /// <summary>
        /// Attempts to decode position data (uncompressed 19 chars or compressed 13 chars)
        /// and assigns the remainder as comment.
        /// </summary>
        /// <param name="data">The data after the timestamp.</param>
        private void DecodePositionAndComment(string data)
        {
            if (data.Length >= 19)
            {
                var pos = Position.TryDecode(data.Substring(0, 19));
                if (pos != null)
                {
                    Position = pos;
                    Comment = data.Length > 19 ? data.Substring(19) : null;
                    return;
                }
            }

            if (data.Length >= 13)
            {
                var pos = Position.TryDecode(data.Substring(0, 13));
                if (pos != null)
                {
                    Position = pos;
                    Comment = data.Length > 13 ? data.Substring(13) : null;
                    return;
                }
            }

            Comment = data;
        }
    }
}
