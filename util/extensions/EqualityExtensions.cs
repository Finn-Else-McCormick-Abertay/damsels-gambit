using System.Collections.Generic;

namespace DamselsGambit.Util;

public static class EqualityExtensions
{
    public static bool IsAnyOf<TSelf, TCompare>(this TSelf self, IEnumerable<TCompare> values) {
        foreach (var compare in values) { if (self.Equals(compare)) return true; }
        return false;
    }
}