using Godot;
using System;
using System.Linq;
using Bridge;
using System.Collections.Generic;
using DamselsGambit.Util;

namespace DamselsGambit;

public enum FocusDirection { Up, Down, Left, Right, None }

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public sealed partial class InputManager : Node
{
	public static class Contexts
	{
		public static readonly GUIDEMappingContext Mouse, Keyboard, Controller;

		public static IEnumerable<GUIDEMappingContext> All => typeof(Contexts).GetFields().Select(x => x.GetValue(null) as GUIDEMappingContext);

		static Contexts() => typeof(Contexts).GetFields().ForEach(field => field.SetValue(null, GUIDEMappingContext.From(ResourceLoader.Load($"res://assets/input/context_{Case.ToSnake(field.Name)}.tres"))));
	}

	public static class Actions
	{
    	public static readonly GUIDEAction Accept, Back, UIDirection, Pause, SelectAt, Cursor, CursorRelative, Drag, Click, ClickHold;

		static Actions() => typeof(Actions).GetFields().ForEach(field => field.SetValue(null, GUIDEAction.From(ResourceLoader.Load($"res://assets/input/actions/{Case.ToSnake(field.Name)}.tres"))));
	}
	
    public static InputManager Instance { get; set; }
	public override void _EnterTree() { if (Instance is not null) throw AutoloadException.For(this); Instance = this; GetTree().Root.Connect(Node.SignalName.Ready, new Callable(this, MethodName.OnTreeReady), (uint)ConnectFlags.OneShot); }
	
	public static bool ShouldOverrideGuiInput { get; set; } = true;
	public static bool ShouldDisplayFocusDebugInfo { get; set; } = OS.HasFeature("debug");
	
	private IEnumerable<GUIDEMappingContext> _enabledContexts = [];

	public static bool IsContextEnabled(GUIDEMappingContext context) => Instance._enabledContexts.Contains(context);
	public static void EnableContext(GUIDEMappingContext context) => GUIDE.EnableMappingContext(context);
	public static void DisableContext(GUIDEMappingContext context) { if (IsContextEnabled(context)) GUIDE.DisableMappingContext(context); }

	private void OnTreeReady() {
		_rootViewport = GetViewport();

		GUIDE.Initialise(GetTree().Root.GetNode("GUIDE"));
		GUIDE.InputMappingsChanged += static () => Instance._enabledContexts = Contexts.All.Where(GUIDE.IsMappingContextEnabled);

		Input.JoyConnectionChanged += static (device, connected) => { if (connected) EnableContext(Contexts.Controller); };

		//Actions.Click.Started += static () => Instance._rootViewport.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
		//Actions.Click.Completed += static () => Instance._rootViewport.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });

		Actions.UIDirection.Triggered += OnUIDirectionTriggered;
		Actions.Accept.Triggered += OnAcceptTriggered;
		Actions.Accept.Completed += OnAcceptCompleted;
		Actions.Back.Triggered += OnBackTriggered;

		// Popup window support
		GetTree().NodeAdded += static node => {
			if (node is not Popup popup || Instance._popups.ContainsKey(popup)) return;
			
			Instance._popups.Add(popup, []);
			Instance._popups[popup].Add(Window.SignalName.FocusEntered, () => Instance._viewportStack.Push(popup));
			Instance._popups[popup].Add(Window.SignalName.FocusExited, () => Instance._viewportStack.TryPop(out var _));
			
			foreach (var (signal, action) in Instance._popups[popup]) popup.Connect(signal, action);
		};
		GetTree().NodeAdded += static node => {
			if (node is not Popup popup || Instance._popups.ContainsKey(popup)) return;
			foreach (var (signal, action) in Instance._popups[popup]) popup.Disconnect(signal, action);
			Instance._popups.Remove(popup);
		};
	}

	private readonly Dictionary<Popup, Dictionary<StringName, Action>> _popups = [];

	private Control _prevFocus = null;
	private readonly Stack<NodePath> _focusStack = [];
	
	private Viewport _rootViewport = null;
	private readonly Stack<Viewport> _viewportStack = [];
	public static Viewport FocusedViewport => Instance._viewportStack.TryPeek(out var viewport) ? viewport : Instance._rootViewport;

	public static void PushToFocusStack()  { if (FocusedViewport?.GuiGetFocusOwner() is Control focused) Instance._focusStack.Push(focused.GetPath()); }
	public static void PopFromFocusStack() { if (Instance._focusStack.TryPop(out NodePath restoredFocusPath)) (Instance.GetNode(restoredFocusPath) as Control)?.GrabFocus(); }

	public static void ClearFocus() { FocusedViewport?.GuiGetFocusOwner()?.ReleaseFocus(); }

	private static bool IsFocusable(Control control) => control.FocusMode == Control.FocusModeEnum.All && control.IsVisibleInTree();

	public static Control FindFocusableWithin(Node root, FocusDirection direction = FocusDirection.Right) {
		if (root is IFocusableContainer container) {
			var (nodeNext, viewport) = container.TryGainFocus(direction, FocusedViewport);
			if (FindFocusableWithin(nodeNext, direction) is Control focusNext) {
				if (viewport is not null && viewport != FocusedViewport) {
					Instance._viewportStack.Push(viewport);
					if (ShouldDisplayFocusDebugInfo) Console.Info("Push viewport ", viewport);
				}
				return focusNext;
			}
		}
		if (root is Control controlRoot && IsFocusable(controlRoot)) return controlRoot;
		var viewportChildren = root?.FindChildrenOfType<Viewport>();
		if (root?.FindChildrenWhere<Control>(x => x.FocusMode == Control.FocusModeEnum.All && x.IsVisibleInTree() && !viewportChildren.Any(viewport => viewport.IsAncestorOf(x)))?.AsEnumerable() is IEnumerable<Control> validChildren) {
			foreach (var child in direction.IsAnyOf(FocusDirection.Left, FocusDirection.Up) ? validChildren.Reverse() : validChildren)
				if (FindFocusableWithin(child) is Control childFocusNext) return childFocusNext;
		}
		return null;
	}

	public static Control GetNextFocus(FocusDirection direction, Control root) {
		if (!root.IsValid()) return null;

		var nextPath = direction switch {
			FocusDirection.Up => root.FocusNeighborTop, FocusDirection.Down => root.FocusNeighborBottom,
			FocusDirection.Left => root.FocusNeighborLeft, FocusDirection.Right => root.FocusNeighborRight,
			FocusDirection.None => root.FocusNext
		};
		
		IEnumerable<string> tags = []; Dictionary<string, string> meta = [];
		bool HasTag(string tag, bool caseSensitive = false) => tags.Any(x => x.Match(tag, caseSensitive)) || meta.Any(x => x.Key.Match(tag, caseSensitive) && bool.Parse(x.Value) == true);

		if (nextPath.ToString() is string pathString && pathString.Contains('!')) {
			var bangIndex = pathString.Find('!'); string beforeBang = pathString[..bangIndex]; string afterBang = bangIndex == pathString.Length - 1 ? "" : pathString[(bangIndex+1)..];
			nextPath = beforeBang; tags = afterBang.Split(',').Select(x => x.Trim());
			meta = tags.Where(x => x.Contains('=')).Select(x => { var equalsIndex = x.Find('='); return KeyValuePair.Create(x[..equalsIndex].Trim(), equalsIndex == x.Length - 1 ? "" : x[(equalsIndex+1)..].Trim()); }).ToDictionary();
		}
		
		if (ShouldDisplayFocusDebugInfo) Console.Info($"Next path: {nextPath.ToPrettyString()}, Tags: {string.Join(", ", tags)}");

		if (HasTag("return")) return Instance._prevFocus;

		if (HasTag("left")) direction = FocusDirection.Left; if (HasTag("right")) direction = FocusDirection.Right; if (HasTag("up")) direction = FocusDirection.Up; if (HasTag("down")) direction = FocusDirection.Down;

		if (!nextPath.IsEmpty && FindFocusableWithin(root.GetNode(nextPath), direction) is Control focusable) return focusable;

		foreach (var parent in root.FindParentsWhere(x => x is Container || x is IFocusableContainer)) {
			var child = parent.FindChildChainTo(root).FirstOrDefault();
			if (parent is IFocusableContainer container && container.GetNextFocus(direction, child) is Node nextNode && FindFocusableWithin(nextNode, direction) is Control nextFocus) return nextFocus;
			if (StandardContainerFocusLogic.GetNextFocus(root, parent, direction, child) is Node standardNextNode && FindFocusableWithin(standardNextNode, direction) is Control standardNextFocus) return standardNextFocus;
			if (parent is Control && GetNextFocus(direction, parent as Control) is Control controlNextFocus) return controlNextFocus;
		}
		return null;
	}

	private void OnAcceptTriggered() {
		var focused = FocusedViewport?.GuiGetFocusOwner();
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
		var focused = FocusedViewport?.GuiGetFocusOwner();
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
			_ when axis.X > threshold => FocusDirection.Right, _ when axis.X < -threshold => FocusDirection.Left,
			_ when axis.Y > threshold => FocusDirection.Up, _ when axis.Y < -threshold => FocusDirection.Down,
			_ => FocusDirection.None
		};
		
		var focused = FocusedViewport?.GuiGetFocusOwner();
		if (focused is null || !focused.IsVisibleInTree()) {
			foreach (var focusContext in GetTree().Root.FindChildrenWhere(x => x is IFocusContext).Select(x => x as IFocusContext)
					.Where(x => x.FocusContextPriority >= 0 && ((x as CanvasItem)?.IsVisibleInTree() ?? true)).OrderBy(x => x.FocusContextPriority)) {
				if (FindFocusableWithin(focusContext?.GetDefaultFocus(direction)) is Control contextDefaultFocus) { contextDefaultFocus?.GrabFocus(); return; }
			}
		}

		if (direction != FocusDirection.None && !((focused is IFocusOverride focusOverride && focusOverride.UseDirectionalInput(direction)) || StandardContainerFocusLogic.UseDirectionalInput(focused, direction))) {
			Control focusNext = GetNextFocus(direction, focused);

			if (focused is not null) {
				foreach (var focusableContainer in focused?.FindParentsWhere(x => x is IFocusableContainer).OrderBy(x => x.FindDistanceToChild(focused)).Select(x => x as IFocusableContainer) ?? []) {
					if (focusNext is not null && !(focusableContainer as Node).IsAncestorOf(focusNext)) {
						if (!focusableContainer.TryLoseFocus(direction, out bool popViewport)) return;
						if (popViewport) {
							_viewportStack.TryPop(out var poppedViewport);
							if (ShouldDisplayFocusDebugInfo) Console.Info($"Pop viewport {poppedViewport}");
						}
					}
				}
			}

			Instance._prevFocus = focused;
			if (ShouldDisplayFocusDebugInfo) Console.Info(focusNext is not null ? $"Shifted focus {Enum.GetName(direction)} to {focusNext.ToPrettyString()} in {FocusedViewport.ToPrettyString()}." : $"Could not shift focus {Enum.GetName(direction)} from {focused.ToPrettyString()}.");
			focusNext?.GrabFocus();
		}
	}

	private static readonly IEnumerable<StringName> _uiActionsToCatch = [ "ui_left", "ui_right", "ui_up", "ui_down", "ui_select", "ui_accept" ];

    public override void _Input(InputEvent @event) {
		if (ShouldOverrideGuiInput && _uiActionsToCatch.Any(action => @event.IsAction(action))) {
			_rootViewport.SetInputAsHandled(); foreach (var viewport in _viewportStack) viewport?.SetInputAsHandled();
			GUIDE.InjectInput(@event);
		}

		switch (@event) {
			case InputEventKey when !IsContextEnabled(Contexts.Keyboard): {
				EnableContext(Contexts.Keyboard); DisableContext(Contexts.Controller);
				GUIDE.InjectInput(@event);
			} break;
			case InputEventJoypadButton or InputEventJoypadMotion when !IsContextEnabled(Contexts.Controller): {
				EnableContext(Contexts.Controller); DisableContext(Contexts.Keyboard);
				GUIDE.InjectInput(@event);
			} break;
			case InputEventMouse when !IsContextEnabled(Contexts.Mouse): {
				EnableContext(Contexts.Mouse);
				GUIDE.InjectInput(@event);
			} break;
		}
    }
}
