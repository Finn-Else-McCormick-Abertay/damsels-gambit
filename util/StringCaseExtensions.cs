using System.Text.RegularExpressions;

namespace DamselsGambit.Util;

public static partial class StringCaseExtensions
{
    public static string PascalToSnakeCase(this string value) {
        if (string.IsNullOrEmpty(value)) return value;
        return PascalSeparatorRegex().Replace(value, "_$1").Trim().ToLower();
    }

    public static string PascalToKebabCase(this string value) {
        if (string.IsNullOrEmpty(value)) return value;
        return PascalSeparatorRegex().Replace(value, "-$1").Trim().ToLower();
    }

    public static string SnakeToKebabCase(this string value) {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Replace('_', '-');
    }

    public static string KebabToSnakeCase(this string value) {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Replace('-', '_');
    }

    public static string SnakeToPascalCase(this string value) {
        if (string.IsNullOrEmpty(value)) return value;
        return SnakeToPascalRegex().Replace(value, "\\U$2$3").Trim();
    }

    public static string KebabToPascalCase(this string value) {
        if (string.IsNullOrEmpty(value)) return value;
        return KebabToPascalRegex().Replace(value, "\\U$2$3").Trim();
    }

    public static string SnakeToCamelCase(this string value) {
        if (string.IsNullOrEmpty(value)) return value;
        return SnakeToCamelRegex().Replace(value, "\\U$1").Trim();
    }

    public static string KebabToCamelCase(this string value) {
        if (string.IsNullOrEmpty(value)) return value;
        return KebabToCamelRegex().Replace(value, "\\U$1").Trim();
    }

    [GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])", RegexOptions.Compiled)]
    private static partial Regex PascalSeparatorRegex();

    [GeneratedRegex("((^\\w)|(?:_)(\\w))", RegexOptions.Compiled)]
    private static partial Regex SnakeToPascalRegex();
    
    [GeneratedRegex("((^\\w)|(?:-)(\\w))", RegexOptions.Compiled)]
    private static partial Regex KebabToPascalRegex();

    [GeneratedRegex("(?:_)(\\w)", RegexOptions.Compiled)]
    private static partial Regex SnakeToCamelRegex();
    
    [GeneratedRegex("(?:-)(\\w)", RegexOptions.Compiled)]
    private static partial Regex KebabToCamelRegex();
}