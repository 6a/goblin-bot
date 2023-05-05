namespace ChimpinOut.GoblinBot.Common
{
    public static class StringHelpers
    {
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
    }
}