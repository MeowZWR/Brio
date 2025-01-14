namespace Brio.Core;

internal static class IntExtensions
{
    public static string ToBrioName(this int i)
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
}
