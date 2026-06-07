namespace AprsSharp.AprsParser
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using AprsSharp.AprsParser.Extensions;

    /// <summary>
    /// Represents an info field for packet type <see cref="PacketType.Message"/>.
    /// </summary>
    public class MessageInfo : InfoField
    {
        /// <summary>
        /// The list of characters which may not be used in the message content.
        /// </summary>
        private static readonly char[] ContentDisallowedChars = { '|', '~', '{' };

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageInfo"/> class.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of a <see cref="MessageInfo"/>.</param>
        public MessageInfo(string encodedInfoField)
            : base(encodedInfoField)
        {
            if (string.IsNullOrWhiteSpace(encodedInfoField))
            {
                throw new ArgumentNullException(nameof(encodedInfoField));
            }

            if (Type != PacketType.Message)
            {
                throw new ArgumentException($"Packet encoding not of type {nameof(PacketType.Message)}. Type was {Type}", nameof(encodedInfoField));
            }

            Match match = Regex.Match(encodedInfoField, RegexStrings.MessageWithId);
            if (!match.Success)
            {
                // Try lenient regex - allows any addressee chars, content with colons/tildes
                match = Regex.Match(encodedInfoField, RegexStrings.MessageLenient);
            }

            if (match.Success)
            {
                Addressee = match.Groups[1].Value.TrimEnd();

                if (match.Groups[2].Success)
                {
                    Content = match.Groups[2].Value;
                }

                if (match.Groups[4].Success)
                {
                    Id = match.Groups[4].Value;
                }
            }
            else
            {
                // Last resort: no second colon found. Extract what we can.
                // Format: ":XXXXXXXXX..." with no second colon
                if (encodedInfoField.Length > 1)
                {
                    var afterFirstColon = encodedInfoField.Substring(1);
                    var secondColon = afterFirstColon.IndexOf(':');
                    if (secondColon >= 0 && secondColon <= 9)
                    {
                        Addressee = afterFirstColon.Substring(0, secondColon).TrimEnd();
                        Content = afterFirstColon.Substring(secondColon + 1);
                    }
                    else
                    {
                        // Truly unparseable as a message - take first 9 chars as addressee
                        Addressee = afterFirstColon.Length > 9
                            ? afterFirstColon.Substring(0, 9).TrimEnd()
                            : afterFirstColon.TrimEnd();
                        Content = afterFirstColon.Length > 9 ? afterFirstColon.Substring(9) : null;
                    }
                }
                else
                {
                    throw new ArgumentException("Did not match RegexStrings.Message");
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageInfo"/> class.
        /// </summary>
        /// <param name="addressee">The station to which this message is addressed.</param>
        /// <param name="content">The content of the message, optional.</param>
        /// <param name="messageId">An ID for the message, optional. If supplied, this requests acknowledgement.</param>
        public MessageInfo(string addressee, string? content, string? messageId)
            : base(PacketType.Message)
        {
            if (string.IsNullOrEmpty(addressee))
            {
                throw new ArgumentNullException(nameof(addressee));
            }
            else if (addressee.Length > 9)
            {
                throw new ArgumentException("Addressee string may have maximum length 9", nameof(addressee));
            }

            if (messageId != null)
            {
                if (messageId.Length == 0 || messageId.Length > 5)
                {
                    throw new ArgumentException("If provided, ID must be of length (0,5]", nameof(messageId));
                }
                else if (!Regex.IsMatch(messageId, RegexStrings.Alphanumeric))
                {
                    throw new ArgumentException("If provided, ID must be only alphanumeric", nameof(messageId));
                }
            }

            if (content != null)
            {
                if (content.Length > 67)
                {
                    throw new ArgumentException("Message content must be 67 characters or less", nameof(content));
                }
                else if (content.IndexOfAny(ContentDisallowedChars) != -1)
                {
                    throw new ArgumentException("Message content may not include `|`, `~`, or `{`", nameof(content));
                }
            }

            Addressee = addressee;
            Content = content;
            Id = messageId;
        }

        /// <summary>
        /// Gets the addressee of the message.
        /// </summary>
        public string Addressee { get; }

        /// <summary>
        /// Gets the message content.
        /// </summary>
        public string? Content { get; }

        /// <summary>
        /// Gets the message ID, which uniquely identifies a message
        /// between a sender and a receiver.
        /// </summary>
        public string? Id { get; }

        /// <summary>
        /// Attempts to parse an encoded info field as a <see cref="MessageInfo"/>.
        /// Returns null if the string cannot be parsed.
        /// </summary>
        /// <param name="encodedInfoField">A string encoding of a <see cref="MessageInfo"/>.</param>
        /// <returns>A <see cref="MessageInfo"/> if parsing succeeds; otherwise, null.</returns>
        public static MessageInfo? TryParse(string encodedInfoField)
        {
            if (string.IsNullOrWhiteSpace(encodedInfoField) || encodedInfoField.Length < 2)
            {
                return null;
            }

            try
            {
                return new MessageInfo(encodedInfoField);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override string Encode()
        {
            StringBuilder encoded = new StringBuilder();

            encoded.Append($":{Addressee}");

            // Encoded addressee must have length 9 and is padded with
            // spaces if too short
            if (Addressee.Length < 9)
            {
                var spacesToAdd = 9 - Addressee.Length;
                for (var i = 0; i < spacesToAdd; ++i)
                {
                    encoded.Append(' ');
                }
            }

            encoded.Append(':');

            encoded.Append(Content ?? string.Empty);

            if (Id != null)
            {
                encoded.Append('{');
                encoded.Append(Id);
            }

            return encoded.ToString();
        }
    }
}
