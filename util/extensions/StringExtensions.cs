using System.Collections.Generic;
using System.IO;
using Godot;

namespace DamselsGambit.Util;

public static class StringExtensions
{
    public static string StripFront(this string self, char what, bool recursive = false) => self.StartsWith(what) ? recursive ? self[1..].StripFront(what, recursive) : self[1..] : self;
    public static string StripFront(this string self, string what, bool recursive = false) => self.StartsWith(what) ? recursive ? self[what.Length..].StripFront(what, recursive) : self[what.Length..] : self;

    public static string StripBack(this string self, char what, bool recursive = false) => self.EndsWith(what) ? recursive ? self[..^1].StripFront(what, recursive) : self[..^1] : self;
    public static string StripBack(this string self, string what, bool recursive = false) => self.EndsWith(what) ? recursive ? self[..^what.Length].StripBack(what, recursive) : self[..^what.Length] : self;
    
    public static string StripEdges(this string self, char what, bool recursive = false) => self.StripFront(what, recursive).StripBack(what, recursive);
    public static string StripEdges(this string self, string what, bool recursive = false) => self.StripFront(what, recursive).StripBack(what, recursive);
    
    public static string StripEdges(this string self, char whatFront, char whatBack, bool recursive = false) => self.StripFront(whatFront, recursive).StripBack(whatBack, recursive);
    public static string StripEdges(this string self, string whatFront, string whatBack, bool recursive = false) => self.StripFront(whatFront, recursive).StripBack(whatBack, recursive);

    public static string StripExtension(this string self) => self.StripBack(Path.GetExtension(self));

    public static string ReplaceExtension(this string self, string fileExtension) => $"{self.StripExtension()}.{fileExtension.StripFront('.')}";

    public static IEnumerable<int> FindAll(this string self, char what, int from = 0, bool caseSensitive = true) {
        List<int> indicies = [];
        while (true) {
            var index = self.Find(what, from, caseSensitive);
            if (index == -1) break;
            indicies.Add(index);
            from = index + 1;
        }
        return indicies;
    }

    public static IEnumerable<int> FindAll(this string self, string what, bool overlapping = false, int from = 0, bool caseSensitive = true) {
        List<int> indicies = [];
        while (true) {
            var index = self.Find(what, from, caseSensitive);
            if (index == -1) break;
            indicies.Add(index);
            from = index + (overlapping ? 1 : what.Length);
        }
        return indicies;
    }
}