namespace SN.Application.Extensions;

public static class StringConversionExtensions 
{
    public static int ToIntOrDefault(this string value, int defaultValue = 0)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

}
