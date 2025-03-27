using System;
using Godot;

namespace DamselsGambit.Util;

public static class TweenExtensions
{
    // Just a helper to allow you to pass a StringName as the property path without having to call ToString on it (since nodes expose their property names as StringNames, not NodePaths)

    /// <inheritdoc cref="Tween.TweenProperty(GodotObject, NodePath, Variant, double)"/>
    public static PropertyTweener TweenProperty(this Tween self, GodotObject @object, StringName property, Variant finalVal, double duration) => self.TweenProperty(@object, property.ToString(), finalVal, duration);

    /// <inheritdoc cref="Tween.TweenCallback(Callable callback)"/>
    public static CallbackTweener TweenCallback(this Tween self, Action callback) => self.TweenCallback(Callable.From(callback));
}