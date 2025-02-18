using Godot;
using System;
using DamselsGambit.Util;
using System.Linq;
using System.Collections.Generic;
using Bridge;

namespace DamselsGambit;

// Setup by GameManager - bridge to GUIDE GDScript autoload
public sealed class GUIDE
{
    private static GUIDE Instance { get; } = new();
    private GUIDE () {}

    private Node _guide;

    public static bool Initialise(Node GUIDE) {
        if (Instance._guide is not null) throw new Exception("Attempted to init GUIDE more than once");
        Instance._guide = GUIDE;
        return Instance._guide is not null;
    }
    
    public static readonly GUIDEMappingContext MappingContextDefault = GDScriptBridge.As<GUIDEMappingContext>(ResourceLoader.Load("res://assets/input/context_default.tres", "GUIDEMappingContext"));

    private static Variant Call(StringName method, params Variant[] args) {
        if (Instance._guide is null) return new Variant();
        return Instance._guide.Call(method, args);
    }

    //public static void EnableMappingContext(GUIDEMappingContextBridge context, bool disableOthers = false, int priority = 0) => Call(MethodName.enable_mapping_context, GDScriptBridge.Underlying(context), disableOthers, priority);
    //public static void DisableMappingContext(GUIDEMappingContextBridge context) => Call(MethodName.disable_mapping_context, GDScriptBridge.Underlying(context));
    //public static bool IsMappingContextEnabled(GUIDEMappingContextBridge context) => Call(MethodName.is_mapping_context_enabled, GDScriptBridge.Underlying(context)).AsBool();
    //public static IEnumerable<GUIDEMappingContextBridge> GetEnabledMappingContexts() => Call(MethodName.get_enabled_mapping_contexts).AsGodotObjectArray<Resource>().Select(x => GDScriptBridge.As<GUIDEMappingContextBridge>(x));

    private static class MethodName {
        public static StringName inject_input = "inject_input";
        public static StringName set_remapping_config = "set_remapping_config";
        public static StringName enable_mapping_context = "enable_mapping_context";
        public static StringName disable_mapping_context = "disable_mapping_context";
        public static StringName is_mapping_context_enabled = "is_mapping_context_enabled";
        public static StringName get_enabled_mapping_contexts = "get_enabled_mapping_contexts";
    }
}