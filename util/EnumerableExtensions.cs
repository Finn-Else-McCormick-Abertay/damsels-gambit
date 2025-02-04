using System.Linq;
using System.Collections.Generic;

namespace DamselsGambit.Util;

static class EnumerableExtensions
{
    public static bool Any<T>(this IEnumerable<T> self, T value) {
        return self.Any(x => Equals(x, value));
    }
}