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
    	public static readonly GUIDEAction Accept = GUIDEAction.From(ResourceLoader.Load("res://assets/input/actions/accept.tres"));
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
		_rootViewport = GetViewport();
		_focusedViewport = _rootViewport;

		GUIDE.Initialise(GetTree().Root.GetNode("GUIDE"));
        GUIDE.Connect(GUIDE.SignalName.InputMappingsChanged, new Callable(this, MethodName.OnInputMappingsChanged));

		Actions.UIDirection.Connect(GUIDEAction.SignalName.Triggered, new Callable(this, MethodName.OnUIDirectionTriggered), 0);
		//Actions.Accept.Connect(GUIDEAction.SignalName.Started, new Callable(this, MethodName.OnAcceptTriggered), 0);
		Actions.Accept.Connect(GUIDEAction.SignalName.Triggered, new Callable(this, MethodName.OnAcceptTriggered), 0);
		Actions.Accept.Connect(GUIDEAction.SignalName.Completed, new Callable(this, MethodName.OnAcceptCompleted), 0);
		
        GetTree().Connect(SceneTree.SignalName.NodeAdded, new Callable(this, MethodName.OnNodeAddedToTree));
        GetTree().Connect(SceneTree.SignalName.NodeRemoved, new Callable(this, MethodName.OnNodeRemovedFromTree));
	}

	private Viewport _rootViewport = null;
	private Viewport _focusedViewport = null;
	private readonly Dictionary<Popup, Dictionary<StringName, Action>> _popups = [];

	private void OnNodeAddedToTree(Node node) {
		if (node is not Popup popup) return;
		if (_popups.ContainsKey(popup)) return;

		_popups.Add(popup, []);
		_popups[popup].Add(Window.SignalName.FocusEntered, () => {
			_focusedViewport = popup;
		});
		_popups[popup].Add(Window.SignalName.FocusExited, () => {
			_focusedViewport = _rootViewport;
		});

		foreach (var (signal, action) in _popups[popup]) popup.Connect(signal, Callable.From(action));
	}
	private void OnNodeRemovedFromTree(Node node) {
		if (node is not Popup popup) return;
		if (!_popups.ContainsKey(popup)) return;

		foreach (var (signal, action) in _popups[popup]) popup.Disconnect(signal, Callable.From(action));
		_popups.Remove(popup);
	}

	private Control _prevFocus = null;

	private readonly Stack<NodePath> _focusStack = [];
	public void PushToFocusStack() {
		var focused = _focusedViewport.GuiGetFocusOwner();
		if (focused is not null) { _focusStack.Push(focused.GetPath()); }
	}
	public void PopFromFocusStack() {
		if (_focusStack.TryPop(out NodePath path)) {
			var restoredFocus = GetNode(path) as Control;
			restoredFocus?.GrabFocus();
		}
	}

	public enum FocusDirection { Up, Down, Left, Right, None }

	public static Control FindFocusableWithin(Node root, FocusDirection direction = FocusDirection.Right) {
		if (root is null) return null;
		if (root is Control control && control.FocusMode == Control.FocusModeEnum.All) return control;
		var validChildren = root.FindChildrenWhere<Control>(x => x.FocusMode == Control.FocusModeEnum.All);
		if (direction == FocusDirection.Left || direction == FocusDirection.Up) validChildren.Reverse();
		return validChildren.FirstOrDefault();
	}

	private static bool IsHorizontal(FocusDirection direction) => direction switch { FocusDirection.Left or FocusDirection.Right => true, _ => false };
	private static bool IsVertical(FocusDirection direction) => direction switch { FocusDirection.Up or FocusDirection.Down => true, _ => false };
	
	private static bool IsHorizontalAnd<T>(Control control, FocusDirection direction) => direction switch { FocusDirection.Left or FocusDirection.Right when control is T => true, _ => false };
	private static bool IsVerticalAnd<T>(Control control, FocusDirection direction) => direction switch { FocusDirection.Up or FocusDirection.Down when control is T => true, _ => false };

	private static bool IsAxisAnd<LeftRightType, UpDownType>(Control control, FocusDirection direction) =>
		direction switch { FocusDirection.Left or FocusDirection.Right when control is LeftRightType => true, FocusDirection.Up or FocusDirection.Down when control is UpDownType => true, _ => false };

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
			if (IsAxisAnd<HBoxContainer, VBoxContainer>(container, direction)) {
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

	private static bool UseDirectionalInput(Control control, FocusDirection direction) {
		if (control is IFocusOverride focusOverride && focusOverride.UseDirectionalInput(direction)) return true;

		if (control is Slider slider && slider.Editable && slider.Value >= slider.MinValue && slider.Value <= slider.MaxValue && IsAxisAnd<HSlider, VSlider>(control, direction)) {
			var step = slider.Step * direction switch {
				FocusDirection.Left or FocusDirection.Down => -1f,
				FocusDirection.Right or FocusDirection.Up => 1f,
				_ => throw new IndexOutOfRangeException()
			};
			slider.Value += step;
			return true;
		}

		if (control is TabBar tabBar && IsHorizontal(direction)) {
			bool tabSelectSuccessful = direction switch {
				FocusDirection.Left => tabBar.SelectPreviousAvailable(), FocusDirection.Right => tabBar.SelectNextAvailable(),
				_ => throw new IndexOutOfRangeException()
			};
			if (tabSelectSuccessful) return true;
		}

		return false;
	}

	private void OnAcceptTriggered() {
		var focused = _focusedViewport.GuiGetFocusOwner();
		if (focused is null) return;

		if (focused is BaseButton button && !button.Disabled) {
			button.EmitSignal(BaseButton.SignalName.ButtonDown);
			if (button.ActionMode == BaseButton.ActionModeEnum.Press) {
				button.EmitSignal(BaseButton.SignalName.Pressed);
				if (button is OptionButton optionButton) optionButton.ShowPopup();
			}
		}
	}
	private void OnAcceptCompleted() {
		var focused = _focusedViewport.GuiGetFocusOwner();
		if (focused is null) return;

		if (focused is BaseButton button && !button.Disabled) {
			button.EmitSignal(BaseButton.SignalName.ButtonUp);
			if (button.ActionMode == BaseButton.ActionModeEnum.Release) {
				button.EmitSignal(BaseButton.SignalName.Pressed);
				if (button is OptionButton optionButton) optionButton.ShowPopup();
			}
		}
	}

	private void OnUIDirectionTriggered() {
		const float threshold = 0.5f;
		var axis = Actions.UIDirection.ValueAxis2d;
		FocusDirection direction = axis switch {
			_ when axis.X > threshold => FocusDirection.Right,
			_ when axis.X < -threshold => FocusDirection.Left,
			_ when axis.Y > threshold => FocusDirection.Up,
			_ when axis.Y < -threshold => FocusDirection.Down,
			_ => FocusDirection.None
		};
		
		var focused = _focusedViewport.GuiGetFocusOwner();
		if (focused is null) {
			var focusContext = GetTree().Root.FindChildWhere(x => x is IFocusContext) as IFocusContext;
			FindFocusableWithin(focusContext?.GetDefaultFocus(direction))?.GrabFocus();
			return;
		}

		if (direction != FocusDirection.None && !UseDirectionalInput(focused, direction)) {
			Control focusNext = GetNextFocus(direction, focused);
			Instance._prevFocus = focused;
			focusNext?.GrabFocus();
		}
	}
	
    private bool _keyboardAndMouseContextEnabled = false;
    private bool _controllerContextEnabled = false;

    private void OnInputMappingsChanged() {
        _keyboardAndMouseContextEnabled = GUIDE.IsMappingContextEnabled(Contexts.KeyboardAndMouse);
        _controllerContextEnabled = GUIDE.IsMappingContextEnabled(Contexts.Controller);
    }

    public override void _Input(InputEvent @event) {
		if (ShouldOverrideGuiInput && new List<StringName>{ UIInput.UiLeft, UIInput.UiRight, UIInput.UiUp, UIInput.UiDown, UIInput.UiSelect, UIInput.UiAccept }.Any(x => @event.IsAction(x))) {
			_focusedViewport.SetInputAsHandled();
			if (_focusedViewport != _rootViewport) _rootViewport.SetInputAsHandled();
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
		public static readonly StringName UiSelect = "ui_select";
		public static readonly StringName UiAccept = "ui_accept";
	}
}
