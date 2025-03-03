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
	public static class Contexts
	{
		public static readonly GUIDEMappingContext KeyboardAndMouse = GUIDEMappingContext.From(ResourceLoader.Load("res://assets/input/context_keyboard_mouse.tres"));
		public static readonly GUIDEMappingContext Controller = GUIDEMappingContext.From(ResourceLoader.Load("res://assets/input/context_controller.tres"));
	}

	public static class Actions
	{
    	public static readonly GUIDEAction Select = GUIDEAction.From(ResourceLoader.Load("res://assets/input/actions/select.tres"));
    	public static readonly GUIDEAction SelectAt = GUIDEAction.From(ResourceLoader.Load("res://assets/input/actions/select_at.tres"));
    	public static readonly GUIDEAction UIDirection = GUIDEAction.From(ResourceLoader.Load("res://assets/input/actions/ui_direction.tres"));
	}

	public static void InjectInput(InputEvent @event) => Call(MethodName.InjectInput, Variant.From(@event));
	public static void SetRemappingConfig(GUIDERemappingConfig remappingConfig) => Call(MethodName.SetRemappingConfig, Variant.From(remappingConfig.InnerObject));

	public static void EnableMappingContext(GUIDEMappingContext context, bool disableOthers = false, int priority = 0) {
		Call(MethodName.EnableMappingContext, Variant.From(context.InnerObject), disableOthers, priority);
		Console.Info($"Enabled input context {context.DisplayName}");
	}
	public static void DisableMappingContext(GUIDEMappingContext context) {
		Call(MethodName.DisableMappingContext, Variant.From(context.InnerObject));
		Console.Info($"Disabled input context {context.DisplayName}");
	}
	public static bool IsMappingContextEnabled(GUIDEMappingContext context) => Call(MethodName.IsMappingContextEnabled, Variant.From(context.InnerObject)).AsBool();
	public static IEnumerable<GUIDEMappingContext> GetEnabledMappingContexts() => Call(MethodName.GetEnabledMappingContexts).AsGodotArray<Resource>().Select(GUIDEMappingContext.From);

	public static event Action InputMappingsChanged {
		add => Connect(SignalName.InputMappingsChanged, Callable.From(value));
		remove => Disconnect(SignalName.InputMappingsChanged, Callable.From(value));
	}

	private static GUIDE Instance { get; } = new(); private GUIDE () {}

	private Node _guide;
	public static Variant Call(StringName method, params Variant[] args) => Instance._guide.Call(method, args);
	public static void Connect(StringName signal, Callable callable) => Instance._guide.Connect(signal, callable);
	public static void Disconnect(StringName signal, Callable callable) => Instance._guide.Disconnect(signal, callable);

	// Should only be called from GameManager
	public static bool Initialise(Node GUIDE) {
		if (Instance._guide is not null) throw new Exception("Attempted to init GUIDE singleton more than once.");
		Instance._guide = GUIDE;
		return Instance._guide is not null;
	}

	public static class MethodName
	{
		public static readonly StringName InjectInput = "inject_input";
		public static readonly StringName SetRemappingConfig = "set_remapping_config";
		public static readonly StringName EnableMappingContext = "enable_mapping_context";
		public static readonly StringName DisableMappingContext = "disable_mapping_context";
		public static readonly StringName IsMappingContextEnabled = "is_mapping_context_enabled";
		public static readonly StringName GetEnabledMappingContexts = "get_enabled_mapping_contexts";
	}

	public static class SignalName
	{
		public static readonly StringName InputMappingsChanged = "input_mappings_changed";
	}
}
