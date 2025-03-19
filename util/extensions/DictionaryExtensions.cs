
using System.Collections.Generic;
using System.Linq;

namespace DamselsGambit.Util;

public static class DictionaryExtensions
{
    public static TValue GetValueOr<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, TValue fallback) {
        bool found = self.TryGetValue(key, out TValue value);
        if (found) return value; else return fallback;
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