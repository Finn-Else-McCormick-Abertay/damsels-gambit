using System;
using Godot;

namespace DamselsGambit.Util;

public static class TweenExtensions
{
    /// <inheritdoc cref="Tween.TweenProperty(GodotObject, NodePath, Variant, double)"/>
    public static PropertyTweener TweenProperty<[MustBeVariant]T>(this Tween self, GodotObject @object, string property, T finalVal, double duration) => self.TweenProperty(@object, property, Variant.From(finalVal), duration);

    /// <inheritdoc cref="Tween.TweenProperty(GodotObject, NodePath, Variant, double)"/>
    public static PropertyTweener TweenProperty(this Tween self, GodotObject @object, StringName property, Variant finalVal, double duration) => self.TweenProperty(@object, property.ToString(), finalVal, duration);

    /// <inheritdoc cref="Tween.TweenProperty(GodotObject, NodePath, Variant, double)"/>
    public static PropertyTweener TweenProperty<[MustBeVariant]T>(this Tween self, GodotObject @object, StringName property, T finalVal, double duration) => self.TweenProperty(@object, property, Variant.From(finalVal), duration);

    /// <inheritdoc cref="Tween.TweenCallback(Callable)"/>
    public static CallbackTweener TweenCallback(this Tween self, Action callback) => self.TweenCallback(Callable.From(callback));
    
    /// <inheritdoc cref="Tween.TweenMethod(Callable, Variant, Variant, double)"/>
    public static MethodTweener TweenMethod(this Tween self, Action<Variant> callback, Variant from, Variant to, double duration) => self.TweenMethod(Callable.From(callback), from, to, duration);

    /// <inheritdoc cref="Tween.TweenMethod(Callable, Variant, Variant, double)"/>
    public static MethodTweener TweenMethod<[MustBeVariant]T>(this Tween self, Action<T> callback, T from, T to, double duration) => self.TweenMethod(Callable.From((Variant x) => callback(x.As<T>())), Variant.From(from), Variant.From(to), duration);
}