using System.Linq;
using System.Collections.Generic;
using System;

namespace DamselsGambit.Util;

static class EnumerableExtensions
{
    public static bool Any<T>(this IEnumerable<T> self, T value) => self.Any(x => Equals(x, value));

    public static void ForEach<T>(this IEnumerable<T> self, Action<T> action) { foreach (var item in self) action(item); }
    
    public static IEnumerable<T> ForEachChained<T>(this IEnumerable<T> self, Action<T> action) { self.ForEach(action); return self; }

    public static IEnumerable<T> WhereExists<T>(this IEnumerable<T> self) => self.Where(x => x is not null);

    // This does the same thing as Reverse, it only exists because some derivatives of IEnumerable hide reverse behind a version that returns void just to be assholes.
    public static IEnumerable<T> Reversed<T>(this IEnumerable<T> self) => self.Reverse();
}