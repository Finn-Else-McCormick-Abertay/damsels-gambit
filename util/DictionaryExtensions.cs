
using System.Collections.Generic;
using System.Linq;

namespace DamselsGambit.Util;

public static class DictionaryExtensions
{
    public static bool TryGetValueOr<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, out TValue value, TValue fallback) {
        bool found = self.TryGetValue(key, out TValue valTemp);
        if (found) { value = valTemp; } else { value = fallback; }
        return found;
    }

    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, TValue value) {
        self.TryAdd(key, value);
        return self[key];
    }
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key) {
        self.TryAdd(key, default);
        return self[key];
    }
}