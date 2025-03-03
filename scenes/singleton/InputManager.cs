using Godot;
using System;
using System.Linq;
using Bridge;
using System.Collections.Generic;
using DamselsGambit.Util;

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
	
	public static bool ShouldOverrideGuiInput { get; set; } = true;

    public static InputManager Instance { get; set; }

	public override void _EnterTree() {
        if (Instance is not null) throw AutoloadException.For(this);
        Instance = this;
		void OnTreeReadyCallback() { OnTreeReady(); GetTree().Root.Ready -= OnTreeReadyCallback; } GetTree().Root.Ready += OnTreeReadyCallback;
    }

	private void OnTreeReady() {
		GUIDE.Initialise(GetTree().Root.GetNode("GUIDE"));
        GUIDE.Connect(GUIDE.SignalName.InputMappingsChanged, new Callable(this, MethodName.OnInputMappingsChanged));

		Actions.UIDirection.Connect(GUIDEAction.SignalName.Triggered, new Callable(this, MethodName.OnUIDirectionTriggered), 0);
	}

	private Control _prevFocus = null;

	private readonly Stack<NodePath> _focusStack = [];
	public void PushToFocusStack() {
		var focused = GetViewport().GuiGetFocusOwner();
		if (focused is not null) { _focusStack.Push(focused.GetPath()); }
	}
	public void PopFromFocusStack() {
		if (_focusStack.TryPop(out NodePath path)) {
			var restoredFocus = GetNode(path) as Control;
			restoredFocus?.GrabFocus();
		}
	}

	public enum FocusDirection { Up, Down, Left, Right }

	private static void ShiftFocus(FocusDirection direction, Control root) {
		if (root is IManualFocus manualFocus && manualFocus.OnFocusShift(direction)) return;

		if (root is TabBar tabBar) {
			if (direction == FocusDirection.Left || direction == FocusDirection.Right) {
				bool tabSelectSuccessful = direction switch {
					FocusDirection.Left => tabBar.SelectPreviousAvailable(), FocusDirection.Right => tabBar.SelectNextAvailable(),
					_ => throw new IndexOutOfRangeException()
				};
				if (tabSelectSuccessful) return;
			}
		}

		Control focusNext = GetNextFocus(direction, root);
		Instance._prevFocus = Instance.GetViewport().GuiGetFocusOwner();
		focusNext?.GrabFocus();
	}

	public static Control FindFocusableWithin(Node root, FocusDirection direction) {
		if (root is null) return null;
		if (root is Control control && control.FocusMode == Control.FocusModeEnum.All) return control;
		var validChildren = root.FindChildrenWhere<Control>(x => x.FocusMode == Control.FocusModeEnum.All);
		if (direction == FocusDirection.Left || direction == FocusDirection.Up) validChildren.Reverse();
		return validChildren.FirstOrDefault();
	}

	public static Control GetNextFocus(FocusDirection direction, Control root) {
		var nextPath = direction switch {
			FocusDirection.Up => root.FocusNeighborTop,
			FocusDirection.Down => root.FocusNeighborBottom,
			FocusDirection.Left => root.FocusNeighborLeft,
			FocusDirection.Right => root.FocusNeighborRight,
			_ => throw new IndexOutOfRangeException()
		};
		if (nextPath == "!return") return Instance._prevFocus;

		if (!nextPath.IsEmpty) return FindFocusableWithin(root.GetNode(nextPath), direction);

		foreach (var container in root.FindParentsOfType<Container>()) {
			var chain = container.FindChildChainTo(root);
			if (((direction == FocusDirection.Left || direction == FocusDirection.Right) && container is HBoxContainer) || ((direction == FocusDirection.Up || direction == FocusDirection.Down) && container is VBoxContainer)) {
				var index = chain.First().GetIndex();
				var nextIndex = direction switch {
					FocusDirection.Left => index - 1, FocusDirection.Right => index + 1,
					FocusDirection.Up => index - 1, FocusDirection.Down => index + 1,
					_ => throw new IndexOutOfRangeException()
				};
				if (nextIndex >= 0 && nextIndex < container.GetChildCount()) {
					var nextFocus = FindFocusableWithin(container.GetChild(nextIndex), direction);
					if (nextFocus is not null) return nextFocus;
				}
			}

			if (container is TabContainer tabContainer) {
				if (direction == FocusDirection.Down && root is TabBar) {
					var nextFocus = FindFocusableWithin(tabContainer.GetCurrentTabControl(), direction);
					if (nextFocus is not null) return nextFocus;
				}
				if (direction == FocusDirection.Up) return tabContainer.GetTabBar();
			}

			if (container is IFocusableContainer focusableContainer) {
				var index = chain.First().GetIndex();
				var nextFocus = focusableContainer.GetNextFocus(direction, index);
				if (nextFocus is not null) return nextFocus;
			}

			var containerNextFocus = GetNextFocus(direction, container);
			if (containerNextFocus is not null) return containerNextFocus;
		}
		return null;
	}

	private void OnUIDirectionTriggered() {
		var focused = GetViewport().GuiGetFocusOwner();
		if (focused is null) return;
		
		var direction = Actions.UIDirection.ValueAxis3d;

		if (direction.X > 0.9f) ShiftFocus(FocusDirection.Right, focused);
		else if (direction.X < -0.9f) ShiftFocus(FocusDirection.Left, focused);

		if (direction.Y > 0.9f) ShiftFocus(FocusDirection.Up, focused);
		else if (direction.Y < -0.9f) ShiftFocus(FocusDirection.Down, focused);
	}
	
    private bool _keyboardAndMouseContextEnabled = false;
    private bool _controllerContextEnabled = false;

    private void OnInputMappingsChanged() {
        _keyboardAndMouseContextEnabled = GUIDE.IsMappingContextEnabled(Contexts.KeyboardAndMouse);
        _controllerContextEnabled = GUIDE.IsMappingContextEnabled(Contexts.Controller);
    }

    public override void _Input(InputEvent @event) {
		if (ShouldOverrideGuiInput && new List<StringName>{ UIInput.UiLeft, UIInput.UiRight, UIInput.UiUp, UIInput.UiDown }.Any(x => @event.IsAction(x))) {
			GetViewport().SetInputAsHandled();
			GUIDE.InjectInput(@event);
		}

        if (!_keyboardAndMouseContextEnabled && (@event is InputEventKey || @event is InputEventMouse)) {
            GUIDE.EnableMappingContext(Contexts.KeyboardAndMouse, true);
        }
        if (!_controllerContextEnabled && (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)) {
            GUIDE.EnableMappingContext(Contexts.Controller, true);
        }
    }

	private static class UIInput
	{
		public static readonly StringName UiLeft = "ui_left";
		public static readonly StringName UiRight = "ui_right";
		public static readonly StringName UiUp = "ui_up";
		public static readonly StringName UiDown = "ui_down";
	}
}
