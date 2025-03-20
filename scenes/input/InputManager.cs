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
    public static InputManager Instance { get; set; }

	public static class Contexts
	{
		private static GUIDEMappingContext LoadContext(string contextName) => GUIDEMappingContext.From(ResourceLoader.Load($"res://assets/input/context_{Case.ToSnake(contextName)}.tres"));

		public static readonly GUIDEMappingContext Mouse = LoadContext(nameof(Mouse));
		public static readonly GUIDEMappingContext Keyboard = LoadContext(nameof(Keyboard));
		public static readonly GUIDEMappingContext Controller = LoadContext(nameof(Controller));
	}

	public static class Actions
	{
		private static GUIDEAction LoadAction(string actionName) => GUIDEAction.From(ResourceLoader.Load($"res://assets/input/actions/{Case.ToSnake(actionName)}.tres"));

    	public static readonly GUIDEAction Accept = LoadAction(nameof(Accept));
    	public static readonly GUIDEAction Back = LoadAction(nameof(Back));
    	public static readonly GUIDEAction SelectAt = LoadAction(nameof(SelectAt));
    	public static readonly GUIDEAction UIDirection = LoadAction(nameof(UIDirection));
    	public static readonly GUIDEAction Pause = LoadAction(nameof(Pause));
	}
	
	public static bool ShouldOverrideGuiInput { get; set; } = true;

	public static bool ShouldDisplayFocusDebugInfo { get; set; } = OS.HasFeature("debug");
	
	public override void _EnterTree() {
        if (Instance is not null) throw AutoloadException.For(this);
        Instance = this; GetTree().Root.Connect(Node.SignalName.Ready, new Callable(this, MethodName.OnTreeReady), (uint)ConnectFlags.OneShot);
    }

	private void OnTreeReady() {
		_rootViewport = GetViewport();
		_focusedViewport = _rootViewport;

		GUIDE.Initialise(GetTree().Root.GetNode("GUIDE"));
        GUIDE.Connect(GUIDE.SignalName.InputMappingsChanged, new Callable(this, MethodName.OnInputMappingsChanged));

		Actions.UIDirection.Connect(GUIDEAction.SignalName.Triggered, new Callable(this, MethodName.OnUIDirectionTriggered), 0);
		Actions.Accept.Connect(GUIDEAction.SignalName.Triggered, new Callable(this, MethodName.OnAcceptTriggered), 0);
		Actions.Accept.Connect(GUIDEAction.SignalName.Completed, new Callable(this, MethodName.OnAcceptCompleted), 0);

		Actions.Back.Connect(GUIDEAction.SignalName.Triggered, new Callable(this, MethodName.OnBackTriggered), 0);
		
        GetTree().Connect(SceneTree.SignalName.NodeAdded, new Callable(this, MethodName.OnNodeAddedToTree));
        GetTree().Connect(SceneTree.SignalName.NodeRemoved, new Callable(this, MethodName.OnNodeRemovedFromTree));
	}

	private Viewport _rootViewport = null;
	private Viewport _focusedViewport = null;
	private readonly Dictionary<Popup, Dictionary<StringName, Action>> _popups = [];

	private void OnNodeAddedToTree(Node node) {
		if (node is not Popup popup || _popups.ContainsKey(popup)) return;

		_popups.Add(popup, []);
		_popups[popup].Add(Window.SignalName.FocusEntered, () => _focusedViewport = popup);
		_popups[popup].Add(Window.SignalName.FocusExited, () => _focusedViewport = _rootViewport);

		foreach (var (signal, action) in _popups[popup]) popup.Connect(signal, Callable.From(action));
	}
	private void OnNodeRemovedFromTree(Node node) {
		if (node is not Popup popup || !_popups.ContainsKey(popup)) return;

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

	public void ClearFocus() {
		_focusedViewport?.GuiGetFocusOwner()?.ReleaseFocus();
	}

	public enum FocusDirection { Up, Down, Left, Right, None }

	public static Control FindFocusableWithin(Node root, FocusDirection direction = FocusDirection.Right) {
		if (root is null) return null;
		if (root is IFocusableContainer focusableContainer && focusableContainer.TryGainFocus(direction) is Control newFocus) return newFocus;
		if (root is Control control && control.FocusMode == Control.FocusModeEnum.All && control.IsVisibleInTree()) return control;
		var validChildren = root.FindChildrenWhere<Control>(x => x.FocusMode == Control.FocusModeEnum.All && x.IsVisibleInTree());
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
		if (!root.IsValid()) return null;

		var nextPath = direction switch {
			FocusDirection.Up => root.FocusNeighborTop,
			FocusDirection.Down => root.FocusNeighborBottom,
			FocusDirection.Left => root.FocusNeighborLeft,
			FocusDirection.Right => root.FocusNeighborRight,
			_ => throw new IndexOutOfRangeException()
		};
		if (nextPath == "!return") return Instance._prevFocus;

		if (!nextPath.IsEmpty && FindFocusableWithin(root.GetNode(nextPath), direction) is Control focusable) return focusable;

		foreach (var container in root.FindParentsWhere<Control>(x => x is Container || x is IFocusableContainer)) {
			var chain = container.FindChildChainTo(root);

			if (IsAxisAnd<HBoxContainer, VBoxContainer>(container, direction)) {
				var index = chain.First().GetIndex();
				var indexDirection = direction switch {
					FocusDirection.Left or FocusDirection.Up => -1,
					FocusDirection.Right or FocusDirection.Down => 1,
					_ => throw new IndexOutOfRangeException()
				};

				index += indexDirection;
				while (index >= 0 && index < container.GetChildCount()) {
					var nextFocus = FindFocusableWithin(container.GetChild(index), direction);
					if (nextFocus is not null) return nextFocus;
					index += indexDirection;
				}
			}

			if (container is GridContainer gridContainer && gridContainer.Columns > 0) {
				var originalIndex = chain.First().GetIndex();
				var index = originalIndex;
				var indexJump = direction switch {
					FocusDirection.Up => -gridContainer.Columns, FocusDirection.Down => gridContainer.Columns,
					FocusDirection.Left => -1, FocusDirection.Right => 1,
					_ => throw new IndexOutOfRangeException()
				};
				index += indexJump;
				while (index >= 0 && index < container.GetChildCount() && direction switch { FocusDirection.Left => (index + 1) % gridContainer.Columns != 0, FocusDirection.Right => index % gridContainer.Columns != 0, _ => true }) {
					var nextFocus = FindFocusableWithin(container.GetChild(index), direction);
					if (nextFocus is not null) return nextFocus;
					index += indexJump;
				}
				if (index >= container.GetChildCount() && originalIndex < container.GetChildCount() - (container.GetChildCount() % gridContainer.Columns)) {
					var nextFocus = FindFocusableWithin(container.GetChildren().Last(), direction);
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
	
	private void OnBackTriggered() {
		foreach (var backContext in GetTree().Root.FindChildrenWhere(x => x is IBackContext).Select(x => x as IBackContext).Where(x => x.BackContextPriority > 0).OrderBy(x => x.BackContextPriority)) {
			if (backContext.UseBackInput()) return;
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
		if (focused is null || !focused.IsVisibleInTree()) {
			foreach (var focusContext in GetTree().Root.FindChildrenWhere(x => x is IFocusContext).Select(x => x as IFocusContext)
					.Where(x => x.FocusContextPriority >= 0 && ((x as CanvasItem)?.IsVisibleInTree() ?? true)).OrderBy(x => x.FocusContextPriority)) {
				var contextDefaultFocus = FindFocusableWithin(focusContext?.GetDefaultFocus(direction));
				if (contextDefaultFocus is not null) { contextDefaultFocus?.GrabFocus(); return; }
			}
		}

		if (direction != FocusDirection.None && !UseDirectionalInput(focused, direction)) {
			Control focusNext = GetNextFocus(direction, focused);

			foreach (var focusableContainer in focused?.FindParentsWhere(x => x is IFocusableContainer).OrderBy(x => x.FindDistanceToChild(focused)).Select(x => x as IFocusableContainer) ?? []) {
				if (!(focusableContainer as Node).IsAncestorOf(focusNext) && !focusableContainer.TryLoseFocus(direction)) return;
			}

			Instance._prevFocus = focused;
			if (ShouldDisplayFocusDebugInfo) Console.Info(focusNext is not null ? $"Shifted focus {Enum.GetName(direction)} to {focusNext}." : $"Could not shift focus {Enum.GetName(direction)} from {focused}.");
			focusNext?.GrabFocus();
		}
	}

	private readonly Dictionary<GUIDEMappingContext, bool> _contextEnabled = [];

    private void OnInputMappingsChanged() {
		foreach (var context in new GUIDEMappingContext[]{ Contexts.Controller, Contexts.Keyboard, Contexts.Mouse })
			_contextEnabled[context] = GUIDE.IsMappingContextEnabled(context);
	}

    public override void _Input(InputEvent @event) {
		if (ShouldOverrideGuiInput && new List<StringName>{ UIInput.UiLeft, UIInput.UiRight, UIInput.UiUp, UIInput.UiDown, UIInput.UiSelect, UIInput.UiAccept }.Any(x => @event.IsAction(x))) {
			_focusedViewport.SetInputAsHandled(); if (_focusedViewport != _rootViewport) _rootViewport.SetInputAsHandled();
			GUIDE.InjectInput(@event);
		}

        if (!_contextEnabled.GetValueOrDefault(Contexts.Keyboard) && @event is InputEventKey) {
            GUIDE.EnableMappingContext(Contexts.Keyboard);
            if (_contextEnabled.GetValueOrDefault(Contexts.Controller)) GUIDE.DisableMappingContext(Contexts.Controller);
			GUIDE.InjectInput(@event);
        }

        if (!_contextEnabled.GetValueOrDefault(Contexts.Controller) && (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)) {
            GUIDE.EnableMappingContext(Contexts.Controller);
            if (_contextEnabled.GetValueOrDefault(Contexts.Keyboard)) GUIDE.DisableMappingContext(Contexts.Keyboard);
			GUIDE.InjectInput(@event);
        }

		if (!_contextEnabled.GetValueOrDefault(Contexts.Mouse) && @event is InputEventMouse) {
            GUIDE.EnableMappingContext(Contexts.Mouse);
			GUIDE.InjectInput(@event);
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
