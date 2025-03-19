using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SourceGeneratorUtils;

namespace DamselsGambit.Util;

public static partial class Case
{
    public static string ToSnake(string value) => PascalBoundaryRegex().Replace(SeparatorRegex().Replace(value.Trim(), "_"), "_$1").ToLower();

    public static string ToKebab(string value) => PascalBoundaryRegex().Replace(SeparatorRegex().Replace(value.Trim(), "-"), "-$1").ToLower();

    public static string ToPascal(string value) => PascalSeparatorRegex().Replace(value.Trim(), match => match.Groups.GetValueOrDefault("1")?.Value?.ToUpper());

    public static string ToCamel(string value) => ToPascal(value).FirstCharToLowerInvariant();

    [GeneratedRegex("[\\s-_]+", RegexOptions.Compiled)] private static partial Regex SeparatorRegex();
    [GeneratedRegex("(?:[\\s_-]+|^)(\\w)", RegexOptions.Compiled)] private static partial Regex PascalSeparatorRegex();
    [GeneratedRegex("(?<!^|_)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])", RegexOptions.Compiled)] private static partial Regex PascalBoundaryRegex();
}