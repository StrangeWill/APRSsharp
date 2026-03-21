namespace AprsSharp.AprsParser
{
    using System;
    using AprsSharp.AprsParser.Extensions;

    /// <summary>
    /// Represents an info field for an APRS Item report.
    /// Format: )NAME!POSITION_DATAComment or )NAME_POSITION_DATAComment
    /// where NAME is 3-9 chars (terminated by ! for live or _ for killed),
    /// followed by position data and optional comment. Items have no timestamp.
    /// </summary>
    public class ItemInfo : InfoField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemInfo"/> class.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of an <see cref="ItemInfo"/>.</param>
        public ItemInfo(string encodedInfoField)
            : base(encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField) || encodedInfoField.Length < 2)
            {
                throw new ArgumentException("Invalid item info field", nameof(encodedInfoField));
            }

            var data = encodedInfoField.Substring(1);

            // Find the live/killed delimiter (! or _) — item names are 3-9 chars,
            // so the delimiter can appear at indices 3 through 9 (inclusive).
            // We search from the start to index 9 to handle all valid name lengths.
            int delimIdx = -1;
            for (int i = 0; i < Math.Min(10, data.Length); i++)
            {
                if (data[i] == '!' || data[i] == '_')
                {
                    delimIdx = i;
                    break;
                }
            }

            if (delimIdx < 1)
            {
                throw new ArgumentException("Could not find item name delimiter", nameof(encodedInfoField));
            }

            Name = data.Substring(0, delimIdx);
            IsLive = data[delimIdx] == '!';

            var remainder = data.Substring(delimIdx + 1);

            // Position + comment (same as ObjectInfo but no timestamp)
            DecodePositionAndComment(remainder);
        }

        /// <summary>
        /// Gets the item name (3-9 characters).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the item is live (true) or killed (false).
        /// </summary>
        public bool IsLive { get; }

        /// <summary>
        /// Gets the position of the item.
        /// </summary>
        public Position? Position { get; private set; }

        /// <summary>
        /// Gets the comment from the item report.
        /// </summary>
        public string? Comment { get; private set; }

        /// <summary>
        /// Attempts to parse an encoded info field as an <see cref="ItemInfo"/>.
        /// Returns null if the string cannot be parsed.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of an <see cref="ItemInfo"/>.</param>
        /// <returns>An <see cref="ItemInfo"/> if parsing succeeds; otherwise, null.</returns>
        public static ItemInfo? TryParse(string encodedInfoField)
        {
            if (string.IsNullOrEmpty(encodedInfoField) || encodedInfoField.Length < 2)
            {
                return null;
            }

            try
            {
                return new ItemInfo(encodedInfoField);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override string Encode() => throw new NotSupportedException($"{nameof(ItemInfo)} encoding is not yet supported.");

        /// <summary>
        /// Attempts to decode position data (uncompressed 19 chars or compressed 13 chars)
        /// and assigns the remainder as comment.
        /// </summary>
        /// <param name="data">The data after the name delimiter.</param>
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
