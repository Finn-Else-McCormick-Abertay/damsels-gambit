using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace DamselsGambit.Util;

public static class VariantUtils
{
    public static MethodInfo GetAsMethodFor(Type type) {
        if (type is null) return null;
        if (type switch {
            _ when type == typeof(bool) => nameof(Variant.AsBool),
            _ when type == typeof(byte) => nameof(Variant.AsByte), _ when type == typeof(byte[]) => nameof(Variant.AsByteArray),
            _ when type == typeof(char) => nameof(Variant.AsChar),
            _ when type == typeof(short) => nameof(Variant.AsInt16), _ when type == typeof(ushort) => nameof(Variant.AsUInt16),
            _ when type == typeof(int) => nameof(Variant.AsInt32), _ when type == typeof(uint) => nameof(Variant.AsUInt32), _ when type == typeof(int[]) => nameof(Variant.AsInt32Array),
            _ when type == typeof(long) => nameof(Variant.AsInt64), _ when type == typeof(ulong) => nameof(Variant.AsUInt64), _ when type == typeof(long[]) => nameof(Variant.AsInt64Array),
            _ when type.IsAnyOf(typeof(float), typeof(double)) => nameof(Variant.AsDouble), _ when type == typeof(float[]) => nameof(Variant.AsFloat32Array), _ when type == typeof(double[]) => nameof(Variant.AsFloat64Array),
            _ when type == typeof(string) => nameof(Variant.AsString), _ when type == typeof(string[]) => nameof(Variant.AsStringArray),
            _ when type == typeof(Vector2[]) => nameof(Variant.AsVector2Array), _ when type == typeof(Vector3[]) => nameof(Variant.AsVector3Array), _ when type == typeof(Vector4[]) => nameof(Variant.AsVector4Array),
            _ when type == typeof(Color[]) => nameof(Variant.AsColorArray),
            _ when type.IsAnyOf(
                typeof(Vector2), typeof(Vector2I), typeof(Vector3), typeof(Vector3I), typeof(Vector4), typeof(Vector4I), typeof(Rect2), typeof(Rect2I), typeof(Transform2D), typeof(Transform3D),
                typeof(Plane), typeof(Quaternion), typeof(Aabb), typeof(Basis), typeof(Projection), typeof(Color), typeof(StringName), typeof(NodePath), typeof(Rid), typeof(Callable), typeof(Signal))
                => $"As{type.Name}",
            _ => null
        } is string nonGenericMethodName) return typeof(Variant).GetMethod(nonGenericMethodName);
        try { if (typeof(Variant).GetMethod(nameof(Variant.As))?.MakeGenericMethod(type) is MethodInfo genericMethod) return genericMethod; } catch (ArgumentException) {}
        return null;
    }
}