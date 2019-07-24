using System.Collections.Generic;
using System.Text;

namespace StoryBot.Core.Extensions
{
    /// <summary>
    /// Extension to convert <see cref="Int32"/> to Roman Numerals
    /// </summary>
    public static class Int32ToRoman
    {
        public static readonly Dictionary<int, string> RomanNumerals;

        static Int32ToRoman()
        {
            RomanNumerals = new Dictionary<int, string>
            {
                { 1000, "M" },
                { 900, "CM" },
                { 500, "D" },
                { 400, "CD" },
                { 100, "C" },
                { 90, "XC" },
                { 50, "L" },
                { 40, "XL" },
                { 10, "X" },
                { 9, "IX" },
                { 5, "V" },
                { 4, "IV" },
                { 1, "I" },
            };
        }

        public static string ToRoman(this int number)
        {
            var stringBuilder = new StringBuilder();
            foreach (var x in RomanNumerals)
            {
                while (number >= x.Key)
                {
                    stringBuilder.Append(x.Value);
                    number -= x.Key;
                }
            }
            return stringBuilder.ToString();
        }
    }
}
