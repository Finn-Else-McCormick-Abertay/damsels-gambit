using Godot;
using System;
using System.Linq;
using Bridge;
using System.Collections.Generic;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public sealed partial class InputManager : Node
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

    private static InputManager Instance { get; set; }

	public override void _EnterTree() {
        if (Instance is not null) throw AutoloadException.For(this);
        Instance = this;
		void OnTreeReadyCallback() { OnTreeReady(); GetTree().Root.Ready -= OnTreeReadyCallback; } GetTree().Root.Ready += OnTreeReadyCallback;
    }

	private void OnTreeReady() {
		GUIDE.Initialise(GetTree().Root.GetNode("GUIDE"));
        GUIDE.Connect(GUIDE.SignalName.InputMappingsChanged, new Callable(this, MethodName.OnInputMappingsChanged));
	}
	
    private bool _keyboardAndMouseContextEnabled = false;
    private bool _controllerContextEnabled = false;

    private void OnInputMappingsChanged() {
        _keyboardAndMouseContextEnabled = GUIDE.IsMappingContextEnabled(Contexts.KeyboardAndMouse);
        _controllerContextEnabled = GUIDE.IsMappingContextEnabled(Contexts.Controller);
    }

    public override void _Input(InputEvent @event) {
        if (!_keyboardAndMouseContextEnabled && (@event is InputEventKey || @event is InputEventMouse)) {
            GUIDE.EnableMappingContext(Contexts.KeyboardAndMouse, true);
        }
        if (!_controllerContextEnabled && (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)) {
            GUIDE.EnableMappingContext(Contexts.Controller, true);
        }
    }
}
