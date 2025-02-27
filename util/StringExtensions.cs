
namespace DamselsGambit.Util;

public static class StringExtensions
{
    public static string StripFront(this string self, char what) => self.StartsWith(what) ? self[1..] : self;
    public static string StripFront(this string self, string what) => self.StartsWith(what) ? self[what.Length..] : self;

    public static string StripBack(this string self, char what) => self.EndsWith(what) ? self[..^1] : self;
    public static string StripBack(this string self, string what) => self.StartsWith(what) ? self[..^what.Length] : self;
}