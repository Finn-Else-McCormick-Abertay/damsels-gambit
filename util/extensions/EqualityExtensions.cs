using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace DamselsGambit.Util;

public static class EqualityExtensions
{
    public static bool IsAnyOf<TSelf, TCompare>(this TSelf self, params IEnumerable<TCompare> values) {
        bool selfIsVariant = typeof(TSelf) == typeof(Variant); bool compareIsVariant = typeof(TCompare) == typeof(Variant);
        if (selfIsVariant && !compareIsVariant && VariantUtils.GetAsMethodFor(typeof(TCompare)) is MethodInfo asCompareMethod) return ((TCompare)Convert.ChangeType(asCompareMethod?.Invoke(self, []), typeof(TCompare))).IsAnyOf(values);
        if (!selfIsVariant && compareIsVariant && VariantUtils.GetAsMethodFor(typeof(TSelf)) is MethodInfo asSelfMethod) return self.IsAnyOf(values.Select(x => (TSelf)Convert.ChangeType(asSelfMethod?.Invoke(self, []), typeof(TSelf))));

        foreach (var compare in values) if (self.Equals(compare)) return true;
        return false;
    }
}