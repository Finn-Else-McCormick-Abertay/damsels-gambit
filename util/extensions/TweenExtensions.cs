using System;
using Godot;

namespace DamselsGambit.Util;

public static class TweenExtensions
{
    // Just a helper to allow you to pass a StringName as the property path without having to call ToString on it (since nodes expose their property names as StringNames, not NodePaths)

    /// <inheritdoc cref="Tween.TweenProperty(GodotObject, NodePath, Variant, double)"/>
    public static PropertyTweener TweenProperty(this Tween self, GodotObject @object, StringName property, Variant finalVal, double duration) => self.TweenProperty(@object, property.ToString(), finalVal, duration);

    /// <inheritdoc cref="Tween.TweenCallback(Callable)"/>
    public static CallbackTweener TweenCallback(this Tween self, Action callback) => self.TweenCallback(Callable.From(callback));
    
    /// <inheritdoc cref="Tween.TweenMethod(Callable, Variant, Variant, double)"/>
    public static MethodTweener TweenMethod(this Tween self, Action<Variant> callback, Variant from, Variant to, double duration) => self.TweenMethod(Callable.From(callback), from, to, duration);
}