using System;
using GDBridge;
using Godot;

namespace DamselsGambit.Util;

static class GDScriptBridge
{
    public static T As<T>(GodotObject obj) {
        return obj is null ? default : (T)Activator.CreateInstance(typeof(T), obj)!;
    }
}