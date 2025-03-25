using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;

namespace DamselsGambit.Util;

// This is separate because the reflection nonsense I'm doing here stops working when in a static class for. Really no good reason.
internal partial class ToPrettyStringInternal
{
    public static string ToPrettyString<T>(T val) => val switch {
		null => "null",
        string str => str,
		NodePath nodePath => nodePath switch { _ when nodePath.IsEmpty => "(Empty)", _ => nodePath.ToString() },
		Node node => $"{node.GetType().Name}(\"{node.Name}\")",
		Resource resource => $"{resource.ResourcePath}({resource.GetType().Name})",
		GodotObject obj => $"{obj.GetType().Name}<{obj.GetInstanceId()}>",
		Type type when type.IsEnum => $"enum{{{string.Join(", ", type.GetEnumNames())}}}",
        _ when typeof(T).Name == "KeyValuePair`2" && typeof(T).GenericTypeArguments is Type[] typeArgs && typeArgs.Length == 2
            => typeof(ToPrettyStringInternal).GetMethod(nameof(KeyValuePairToPrettyString))?.MakeGenericMethod(typeArgs)?.Invoke(null, [val]) as string,
        _ when typeof(T).Name.StartsWith("ValueTuple`") && typeof(T).CustomAttributes is IEnumerable<CustomAttributeData> attributes => $"({string.Join(", ", typeof(T).GetFields().Index().Select(pair => $"{ToPrettyString(pair.Item.GetValue(val))}"))})",
		_ when IsEnumerable(typeof(T)) => EnumerableToPrettyString(val, ", "),
        _ => val.ToString()
    };
    public static string KeyValuePairToPrettyString<TKey, TValue>(KeyValuePair<TKey, TValue> val) => $"{ToPrettyString(val.Key)} : {ToPrettyString(val.Value)}";

	private static bool IsEnumerable(Type type) => typeof(IEnumerable).IsAssignableFrom(type) || typeof(IEnumerable<>).IsAssignableFrom(type);
	private static string EnumerableToPrettyString(object obj, string separator = ", ") {
		if (obj is not IEnumerable enumerable) return null;
		var sb = new StringBuilder(); bool first = true;
		foreach(var item in enumerable) { if (!first) sb.Append(separator); first = false; sb.Append(ToPrettyString(item)); }
		return sb.ToString();
	}
}

public static class ToStringExtensions
{
	public static string ToPrettyString<T>(this T self) => ToPrettyStringInternal.ToPrettyString(self);

    public static string ToPrettyString<T>(this IEnumerable<T> self, bool inline = false) =>
        $"{{{(inline ? "" : "\n")}{string.Join(inline ? ", " : ",\n", self.Select(x => $"{(inline ? "" : "\t")}{ToPrettyStringInternal.ToPrettyString(x)}"))}{(inline || !self.Any() ? "" : "\n")}}}";
}