namespace ChimpinOut.GoblinBot.Common
{
    public static class StringHelpers
    {
        /// <summary>
        /// The string below is a single zero-width space (https://en.wikipedia.org/wiki/Zero-width_space)
        /// </summary>
        public const string ZeroWidthSpace = "​";
        
        public static string ToOrdinal(ulong num)
        {
            if (num <= 0)
            {
                return num.ToString();
            }

            switch(num % 100)
            {
                case 11:
                case 12:
                case 13:
                {
                    return num + "ᵗʰ";
                }
                default:
                {
                    return (num % 10) switch
                    {
                        1 => num + "ˢᵗ",
                        2 => num + "ⁿᵈ",
                        3 => num + "ʳᵈ",
                        _ => num + "ᵗʰ"
                    };
                }
            }
        }
        
        public static StringBuilder AppendNicknameToStringBuilder(StringBuilder sb, ulong level, string nickname, bool format = true)
        {
            var boldMarkup = format ? "**" : null;
            var indentMarkup = format ? "> " : null;
            return sb.Append(indentMarkup).Append(boldMarkup).Append("Level ").Append(level).Append(" - ").Append(nickname).Append(boldMarkup);
        }
    }
}