using System.Text;

namespace Brio.Core;

public static class IntExtensions
{
    private static bool UseChineseNamingScheme() => ClientRegionHelper.IsChineseClient();

    private static string ToBrioNameChinese(int i)
    {
        if(i < 0 || i >= 260) return string.Empty;

        char prefix = (char)('A' + (i / 10));
        string numberPart = (i % 10) switch
        {
            0 => "zero",
            1 => "one",
            2 => "two",
            3 => "three",
            4 => "four",
            5 => "five",
            6 => "six",
            7 => "seven",
            8 => "eight",
            9 => "nine",
            _ => ""
        };

        string name = $"{prefix}{numberPart}";
        return name.Length > 6 ? name[..6] : name;
    }

    public static string ToWords(this int number, string separator = " ")
    {
        string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

        StringBuilder result = new();

        if(number < 100)
        {
            if(number < 20)
            {
                result.Append(ones[number]);
            }
            else
            {
                int tenPart = number / 10;
                int onePart = number % 10;
                result.Append(tens[tenPart]);

                if(onePart > 0)
                {
                    result.Append(separator);
                    result.Append(ones[onePart]);
                }
            }
        }
        else
        {
            result.Append(ones[number / 100]);
            result.Append(separator);
            result.Append("Hundred");

            int remainder = number % 100;
            if(remainder > 0)
            {
                result.Append(separator);
                result.Append(ToWords(remainder, separator));
            }
        }

        return result.ToString();
    }

    private static string ToBrioNameInternational(int i)
    {
        string result = ToWords(i, " ");

        if(!result.Contains(' '))
            return "Brio " + result;

        return result;
    }

    public static string ToBrioName(this int i)
    {
        return UseChineseNamingScheme() ? ToBrioNameChinese(i) : ToBrioNameInternational(i);
    }

    public static string ToName(this int i)
    {
        return UseChineseNamingScheme() ? ToBrioNameChinese(i) : ToWords(i, " ");
    }

    public static string ToName(this ulong i)
    {
        return ((int)i).ToName();
    }
}
