using Godot;
using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace DamselsGambit.Util;

public static class FlagsExtensions
{
    public static T SetFlags<T, TFlag>(this T self, params TFlag[] flags) where T : IBinaryInteger<T> where TFlag : Enum { foreach (TFlag flag in flags) self |= (T)Convert.ChangeType(flag, typeof(T)); return self; }
    public static T UnsetFlags<T, TFlag>(this T self, params TFlag[] flags) where T : IBinaryInteger<T> where TFlag : Enum { foreach (TFlag flag in flags) self &= ~(T)Convert.ChangeType(flag, typeof(T)); return self; }

    public static Variant SetFlags<TFlag>(this Variant self, params TFlag[] flags) where TFlag : Enum => Variant.From(self.AsInt64().SetFlags(flags));
    public static Variant UnsetFlags<TFlag>(this Variant self, params TFlag[] flags) where TFlag : Enum => Variant.From(self.AsInt64().UnsetFlags(flags));
}