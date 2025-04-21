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
    public static InputManager Instance { get; set; }
	public override void _EnterTree() { if (Instance is not null) throw AutoloadException.For(this); Instance = this; GetTree().Root.Connect(Node.SignalName.Ready, OnTreeReady, (uint)ConnectFlags.OneShot); }
	
	public static bool ShouldOverrideGuiInput { get; set; } = true;
	public static bool ShouldDisplayFocusDebugInfo { get; set; } = false;//OS.HasFeature("debug");

	public static readonly string InputConfigPath = "user://input_config.tres";

	public static class Actions
	{
    	public static readonly GUIDEAction Play, Discard, Profile, Accept, Back, UIDirection, Pause, SelectAt, Cursor, CursorRelative, Drag, Click, ClickHold;

		// Load all actions during static construction
		static Actions() => typeof(Actions).GetFields().ForEach(field => field.SetValue(null, GUIDEAction.From(ResourceLoader.Load($"res://assets/input/actions/{Case.ToSnake(field.Name)}.tres"))));
	}

	public static class Contexts
	{
		public static readonly GUIDEMappingContext Mouse, Keyboard, Controller;

		public static IEnumerable<GUIDEMappingContext> All => typeof(Contexts).GetFields().Select(x => x.GetValue(null) as GUIDEMappingContext);

		// Load all contexts during static construction
		static Contexts() => typeof(Contexts).GetFields().ForEach(field => field.SetValue(null, GUIDEMappingContext.From(ResourceLoader.Load($"res://assets/input/context_{Case.ToSnake(field.Name)}.tres"))));
	}
	
	// Cache of loaded contexts (so we don't have to constantly query GUIDE through the interop)
	private IEnumerable<GUIDEMappingContext> _enabledContexts = [];

	public static bool IsContextEnabled(GUIDEMappingContext context) => Instance._enabledContexts.Contains(context);
	public static void EnableContext(GUIDEMappingContext context) => GUIDE.EnableMappingContext(context);
	public static void DisableContext(GUIDEMappingContext context) { if (IsContextEnabled(context)) GUIDE.DisableMappingContext(context); }

	// Runs when full tree is ready
	private void OnTreeReady() {
		ProcessMode = ProcessModeEnum.Always;

		_rootViewport = GetViewport();

		// Initialise the C# wrapper for the GUIDE autoload
		GUIDE.Initialise(GetTree().Root.GetNode("GUIDE"));

		// Cache the loaded input mappings when input mappings change so we don't have to do four expensive cross-language calls during every input event
		GUIDE.InputMappingsChanged += static () => Instance._enabledContexts = Contexts.All.Where(GUIDE.IsMappingContextEnabled);

		// Enable controller context when conteroller connects
		Input.JoyConnectionChanged += static (device, connected) => { if (connected) EnableContext(Contexts.Controller); };

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
		GetTree().NodeRemoved += static node => {
			if (node is not Popup popup || !Instance._popups.TryGetValue(popup, out var signals)) return;
			foreach (var (signal, action) in signals) popup.Disconnect(signal, action);
			Instance._popups.Remove(popup);
		};
	}

	// Active popups, and the signals of theirs we need to disconnect when they die
	private readonly Dictionary<Popup, Dictionary<StringName, Action>> _popups = [];

	private Control _prevFocus = null;
	private readonly Dictionary<FocusDirection, Control> _prevFocusFromDirection = [];

	private readonly Stack<NodePath> _focusStack = [];
	
	private Viewport _rootViewport = null;
	private readonly Stack<Viewport> _viewportStack = [];
	public static Viewport FocusedViewport => Instance._viewportStack.TryPeek(out var viewport) ? viewport : Instance._rootViewport;

	public static void PushToFocusStack()  { if (FocusedViewport?.GuiGetFocusOwner() is Control focused) Instance._focusStack.Push(focused.GetPath()); }
	public static void PopFromFocusStack() { if (Instance._focusStack.TryPop(out NodePath restoredFocusPath)) (Instance.GetNode(restoredFocusPath) as Control)?.GrabFocus(); }

	// Release focus for current focused viewport
	public static void ClearFocus() => FocusedViewport?.GuiGetFocusOwner()?.ReleaseFocus();

	// Find focusable control within given node, coming from given direction
	// (eg: HBoxContainer from left gives its first child, from right gives its last child; a button from any direction gives itself; etc.)
	public static Control FindFocusableWithin(Node root, FocusDirection direction = FocusDirection.None) {
		static bool IsFocusable(Control control) => control.FocusMode == Control.FocusModeEnum.All && control.IsVisibleInTree();
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
		if (root?.FindChildrenWhere<Control>(x => IsFocusable(x) && !viewportChildren.Any(viewport => viewport.IsAncestorOf(x)))?.AsEnumerable() is IEnumerable<Control> validChildren) {
			foreach (var child in direction.IsAnyOf(FocusDirection.Left, FocusDirection.Up) ? validChildren.Reverse() : validChildren)
				if (FindFocusableWithin(child) is Control childFocusNext) return childFocusNext;
		}
		return null;
	}

	// Find next focus to jump to in given direction from given control
	// Starts by checking node's FocusNeighbor for the given direction (including extracting special meta info from the NodePath, to enable things like returning to the previous node)
	// Then falls back to ascending up the tree, following input logic from IFocusableContainer or default logic for Godot containers
	public static Control GetNextFocus(FocusDirection direction, Control root) {
		if (!root.IsValid()) return null;

		IEnumerable<(string, Func<string, bool>)> predicates = [
			("exists", arg => root.GetNode(arg) is Node node),
			("visible", arg => root.GetNode(arg) is CanvasItem canvasItem && canvasItem.Visible),
			("from", arg => arg switch {
				"left" when direction == FocusDirection.Right => true, "right" when direction == FocusDirection.Left => true,
				"top" when direction == FocusDirection.Down => true, "bottom" when direction == FocusDirection.Up => true,
				_ => false
			})
		];

		var nextPath = new TaggedNodePath(
			direction switch {
				FocusDirection.Up => root.FocusNeighborTop, FocusDirection.Down => root.FocusNeighborBottom,
				FocusDirection.Left => root.FocusNeighborLeft, FocusDirection.Right => root.FocusNeighborRight, FocusDirection.None => root.FocusNext
			},
			predicates
		);
		
		if (ShouldDisplayFocusDebugInfo) Console.Info($"Next path: {nextPath.RootPath.ToPrettyString()}, Tags: {nextPath.Tags.ToPrettyString()}");

		if (nextPath.HasTag("return")) {
			Control returnFocus = Instance._prevFocus;
			var returnTag = nextPath.GetTag("return");
			bool shouldUse = true;
			if (returnTag.Contains("if")) {
				var split = returnTag.Split("if", 2, StringSplitOptions.TrimEntries);
				if (TaggedNodePath.TryEvaluateCondition(split[1], out bool result, predicates.Append(("within", arg => root.GetNode(arg) is Node node && node.IsAncestorOf(returnFocus))))) shouldUse = result;
				returnTag = split[0];
			}
			if (returnTag.StartsWith("from", StringComparison.CurrentCultureIgnoreCase)) {
				returnFocus = null;
				var fromArgs = returnTag.ToLower().StripFront("from").Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
				foreach (var arg in fromArgs) {
					if (arg == "top" && Instance._prevFocusFromDirection.TryGetValue(FocusDirection.Down, out var topFocus)) returnFocus = topFocus;
					else if (arg == "bottom" && Instance._prevFocusFromDirection.TryGetValue(FocusDirection.Up, out var bottomFocus)) returnFocus = bottomFocus;
					else if (arg == "left" && Instance._prevFocusFromDirection.TryGetValue(FocusDirection.Right, out var leftFocus)) returnFocus = leftFocus;
					else if (arg == "right" && Instance._prevFocusFromDirection.TryGetValue(FocusDirection.Left, out var rightFocus)) returnFocus = rightFocus;
				}
			}
			if (returnFocus is not null && shouldUse) return returnFocus;
		}

		direction = direction switch {
			_ when nextPath.GetComplexFlagOrDefault("from", "left") => FocusDirection.Right, _ when nextPath.GetComplexFlagOrDefault("from", "right") => FocusDirection.Left,
			_ when nextPath.GetComplexFlagOrDefault("from", "top") => FocusDirection.Down, _ when nextPath.GetComplexFlagOrDefault("from", "bottom") => FocusDirection.Up,
			_ => direction
		};

		var rootPath = nextPath.RootPath;
		if (!rootPath.IsEmpty && FindFocusableWithin(root.GetNode(rootPath), direction) is Control focusable) return focusable;
		else {
			foreach (var parent in root.FindParentsWhere(x => x is Container || x is IFocusableContainer)) {
				var child = parent.FindChildChainTo(root).FirstOrDefault();
				if (parent is IFocusableContainer container && container.GetNextFocus(direction, child) is Node nextNode && FindFocusableWithin(nextNode, direction) is Control nextFocus) return nextFocus;
				if (StandardContainerFocusLogic.GetNextFocus(root, parent, direction, child) is Node standardNextNode && FindFocusableWithin(standardNextNode, direction) is Control standardNextFocus) return standardNextFocus;
				if (parent is Control && GetNextFocus(direction, parent as Control) is Control controlNextFocus) return controlNextFocus;
			}
		}
		return null;
	}

	// Connected to Accept action's Triggered signal
	private void OnAcceptTriggered() {
		var focused = FocusedViewport?.GuiGetFocusOwner();
		if (focused is null) return;

		if (focused is BaseButton button && !button.Disabled) {
			button.EmitSignal(BaseButton.SignalName.ButtonDown);
			if (button.ActionMode == BaseButton.ActionModeEnum.Press) { button.EmitSignal(BaseButton.SignalName.Pressed); if (button is OptionButton optionButton) optionButton.ShowPopup(); }
		}
	}
	// Connected to Accept action's Completed signal
	private void OnAcceptCompleted() {
		var focused = FocusedViewport?.GuiGetFocusOwner();
		if (focused is null) return;

		if (focused is BaseButton button && !button.Disabled) {
			button.EmitSignal(BaseButton.SignalName.ButtonUp);
			if (button.ActionMode == BaseButton.ActionModeEnum.Release) { button.EmitSignal(BaseButton.SignalName.Pressed); if (button is OptionButton optionButton) optionButton.ShowPopup(); }
		}
	}
	
	// Connected to Back action
	private void OnBackTriggered() {
		foreach (var backContext in GetTree().Root.FindChildrenWhere(x => x is IBackContext).Select(x => x as IBackContext).Where(x => (x as Node)?.CanProcess() ?? true && x.BackContextPriority > 0).OrderBy(x => x.BackContextPriority)) {
			if (backContext.UseBackInput()) return;
		}
	}

	// Connected to UIDirection action
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
					.Where(x => x.FocusContextPriority >= 0 && ((x as Node)?.CanProcess() ?? true) && ((x as CanvasItem)?.IsVisibleInTree() ?? true)).OrderBy(x => x.FocusContextPriority)) {
				if (FindFocusableWithin(focusContext?.GetDefaultFocus(direction)) is Control contextDefaultFocus) { contextDefaultFocus?.GrabFocus(); return; }
			}
		}

		if (direction != FocusDirection.None && !((focused is IFocusOverride focusOverride && focusOverride.UseDirectionalInput(direction)) || StandardContainerFocusLogic.UseDirectionalInput(focused, direction))) {
			Control focusNext = GetNextFocus(direction, focused);

			foreach (var focusableContainer in focused?.FindParentsWhere(x => x is IFocusableContainer)?.OrderBy(x => x.FindDistanceToChild(focused))?.Select(x => x as IFocusableContainer) ?? []) {
				if (focusNext is not null && !(focusableContainer as Node).IsAncestorOf(focusNext)) {
					if (!focusableContainer.TryLoseFocus(direction, out bool popViewport)) return;
					if (popViewport) { _viewportStack.TryPop(out var poppedViewport); if (ShouldDisplayFocusDebugInfo) Console.Info($"Pop viewport {poppedViewport}"); }
				}
			}

			Instance._prevFocus = focused;
			Instance._prevFocusFromDirection[direction] = focused;
			if (ShouldDisplayFocusDebugInfo) Console.Info(focusNext is not null ? $"Shifted focus {Enum.GetName(direction)} to {focusNext.ToPrettyString()} in {FocusedViewport.ToPrettyString()}." : $"Could not shift focus {Enum.GetName(direction)} from {focused.ToPrettyString()}.");
			focusNext?.GrabFocus();
		}
	}

	private static readonly IEnumerable<StringName> _uiActionsToCatch = [ "ui_left", "ui_right", "ui_up", "ui_down", "ui_select", "ui_accept" ];

    public override void _Input(InputEvent @event) {
		// If currently overriding gui input, handle any inputs which constitute relevant ui actions (so we can handle them via GUIDE)
		// Input is overriden in game, but not in console window
		if (ShouldOverrideGuiInput && _uiActionsToCatch.Any(action => @event.IsAction(action))) {
			_rootViewport.SetInputAsHandled(); foreach (var viewport in _viewportStack) viewport?.SetInputAsHandled();
			GUIDE.InjectInput(@event);
		}

		// Enable context for event type if not already enabled. We inject the input to GUIDE afterwards to avoid the initial input which enables the context being eaten
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
