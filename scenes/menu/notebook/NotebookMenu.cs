using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;

namespace DamselsGambit;

[Tool]
public partial class NotebookMenu : Control, IFocusableContainer, IReloadableToolScript
{
	[Export] public string SuitorName { get; set { field = value; UpdateDialogueViewNode(); } }

	[ExportGroup("States")]

	[ExportSubgroup("Closed", "Closed")]
	[Export] public Vector2 ClosedOffset { get; set { field = value; RestoreAnimationState(); } }
	[Export(PropertyHint.Range, "-90,90,0.001,radians")] public double ClosedCoverAngle { get; set { field = value; RestoreAnimationState(); } } = 0.0;
	
	[ExportSubgroup("Highlight", "Highlight")]
	[Export] public Vector2 HighlightOffset { get; set { field = value; RestoreAnimationState(); } }
	[Export(PropertyHint.Range, "-90,90,0.001,radians")] public double HighlightCoverAngle { get; set { field = value; RestoreAnimationState(); } } = 0.0;
	[Export(PropertyHint.Range, "0,1,or_greater,suffix:s")] private double HighlightDuration { get; set; } = 0.2;
	
	[ExportSubgroup("Open", "Open")]
	[Export] public Vector2 OpenOffset { get; set { field = value; RestoreAnimationState(); } }
	[Export(PropertyHint.Range, "-90,90,0.001,radians")] public double OpenCoverAngle { get; set { field = value; RestoreAnimationState(); } } = 0.0;
	[Export(PropertyHint.Range, "0,1,or_greater,suffix:s")] private double OpenDuration { get; set; } = 0.3;
	
	[ExportSubgroup("Pause Menu", "PauseMenu")]
	[Export] public Vector2 PauseMenuOffset { get; set { field = value; RestoreAnimationState(); } }
	[Export(PropertyHint.Range, "-90,90,0.001,radians")] public double PauseMenuAngle { get; set { field = value; RestoreAnimationState(); } } = 0.0;
	[Export] public float PauseMenuScale { get; set { field = value; RestoreAnimationState(); } } = 1f;
	[Export(PropertyHint.Range, "0,1,or_greater,suffix:s")] private double PauseMenuTurnDuration { get; set; } = 0.3;
	[Export] private Tween.TransitionType PauseMenuTransitionType { get; set; } = Tween.TransitionType.Linear;

	[ExportGroup("Nodes")]
	[Export] private ViewportLayerContainer LayerContainer { get; set { field = value; UpdateLayerReferences(); } }
	[ExportSubgroup("Fallback", "Fallback")]
	[Export] private Control FallbackRoot { get; set { field = value; UpdateLayerReferences(); } }
	[Export] private Control FallbackProfilePage { get; set { field = value; UpdateLayerReferences(); } }
	[Export] private Node3D FallbackCoverPivot { get; set { field = value; UpdateLayerReferences(); } }

	public Control Root { get; private set; }

	public Notebook.ProfilePage ProfilePage {
		get; private set {
			ProfilePage?.PauseButton?.TryDisconnectAll((BaseButton.SignalName.Pressed, TogglePauseMenu));
			ProfilePage?.ProfileButton?.TryDisconnectAll(
				(Control.SignalName.MouseEntered, OnFocus), (Control.SignalName.MouseExited, OnUnfocus),
				(Control.SignalName.FocusEntered, OnFocus), (Control.SignalName.FocusExited, OnUnfocus),
				(BaseButton.SignalName.Pressed, ToggleOpen)
			);
			field = value;
			ProfilePage?.OnReady(page => {
				UpdateDialogueViewNode();
				ProfilePage?.PauseButton?.TryConnectAll((BaseButton.SignalName.Pressed, TogglePauseMenu));
				page.ProfileButton?.ConnectAll(
					(Control.SignalName.MouseEntered, OnFocus), (Control.SignalName.MouseExited, OnUnfocus),
					(Control.SignalName.FocusEntered, OnFocus), (Control.SignalName.FocusExited, OnUnfocus),
					(BaseButton.SignalName.Pressed, ToggleOpen)
				);
			});
		}
	}
	public Viewport ProfileViewport { get; private set; }
	public Node3D CoverPivot { get; private set; }

	[ExportGroup("Debug", "Debug")]
	[Export] private AnimationState DebugState { get => State; set => State = value; }
	
	public enum AnimationState { Closed, Highlighted, Open, PauseMenu };
	public AnimationState State { get; private set { this.OnReady(() => AnimateStateChange(State, value)); field = value; } }

	public bool Open { get; set { field = value; State = State switch { _ when Open => AnimationState.Open, AnimationState.Open when !Open && Highlighted => AnimationState.Highlighted, _ => AnimationState.Closed }; } } = false;
	public bool Highlighted { get; set { field = value; State = State switch { AnimationState.Open => AnimationState.Open, _ when Highlighted => AnimationState.Highlighted, _ => AnimationState.Closed }; } } = false;
	public bool InPauseMenu { get; set { field = value; State = State switch { _ when InPauseMenu => AnimationState.PauseMenu, AnimationState.PauseMenu when Open => AnimationState.Open, AnimationState.PauseMenu when Highlighted => AnimationState.Highlighted, _ => AnimationState.Closed }; } } = false;
	
	private readonly Dictionary<Node, Tween> _tweens = [];
	private Tween CreateTweenFor(Node node) {
		CleanupTweenFor(node);
		if (node.IsInvalid()) return null;
		var tween = node.CreateTween(); _tweens.Add(node, tween);
		return tween;
	}
	private void CleanupTweenFor(Node node) {
		if (node is null) return;
		if (_tweens.TryGetValue(node, out var oldTween) && oldTween.IsValid()) oldTween.Kill();
		_tweens.Remove(node);
	}

	private void AnimateStateChange(AnimationState oldState, AnimationState newState) {
		if (oldState == newState) return;

		double duration = newState switch {
			AnimationState.Open => OpenDuration,
			AnimationState.Highlighted => oldState switch { AnimationState.Open => OpenDuration, AnimationState.Closed => HighlightDuration, AnimationState.PauseMenu => PauseMenuTurnDuration, _ => 0 },
			AnimationState.Closed => oldState switch { AnimationState.Open => OpenDuration, AnimationState.Highlighted => HighlightDuration, AnimationState.PauseMenu => PauseMenuTurnDuration, _ => 0 },
			AnimationState.PauseMenu => PauseMenuTurnDuration
		};

		var position = newState switch { AnimationState.Open => OpenOffset, AnimationState.Highlighted => HighlightOffset, AnimationState.Closed => ClosedOffset, AnimationState.PauseMenu => PauseMenuOffset };
		var rootAngle = newState switch { AnimationState.PauseMenu => PauseMenuAngle, _ => 0 };
		var coverAngle = newState switch { AnimationState.Open => OpenCoverAngle, AnimationState.Highlighted => HighlightCoverAngle, AnimationState.Closed or AnimationState.PauseMenu => ClosedCoverAngle };
		var scale = newState switch { AnimationState.PauseMenu => PauseMenuScale, _ => 1 };

		var rootTween = CreateTweenFor(Root)?.SetParallel();
		rootTween?.TweenProperty(Root, Control.PropertyName.Position, position, duration);
		rootTween?.TweenProperty(Root, Control.PropertyName.Rotation, rootAngle, duration)?.SetEase(Tween.EaseType.InOut)?.SetTrans(PauseMenuTransitionType);
		rootTween?.TweenProperty(Root, Control.PropertyName.Scale, new Vector2(scale, scale), duration)?.SetTrans(PauseMenuTransitionType);

		CreateTweenFor(CoverPivot)?.TweenProperty(CoverPivot, Control.PropertyName.Rotation + ":y", coverAngle, duration);

		ProfilePage?.FadeShadows(newState switch { AnimationState.Open => false, _ => true }, duration);
	}

	private void RestoreAnimationState() {
		if (!IsNodeReady()) return;
		
		ProfilePage?.SetShadows(State switch { AnimationState.Open => false, _ => true });
		
		var position = State switch { AnimationState.Open => OpenOffset, AnimationState.Highlighted => HighlightOffset, AnimationState.Closed => ClosedOffset, AnimationState.PauseMenu => PauseMenuOffset };
		var rootAngle = State switch { AnimationState.PauseMenu => PauseMenuAngle, _ => 0 };
		var coverAngle = State switch { AnimationState.Open => OpenCoverAngle, AnimationState.Highlighted => HighlightCoverAngle, AnimationState.Closed or AnimationState.PauseMenu => ClosedCoverAngle };
		var scale = State switch { AnimationState.PauseMenu => PauseMenuScale, _ => 1 };
		CallableUtils.CallDeferred(() => {
			if (Root.IsValid()) { Root.Position = position; Root.Rotation = (float)rootAngle; Root.Scale = new(scale, scale); }
			if (CoverPivot.IsValid()) CoverPivot.Rotation = CoverPivot.Rotation with { Y = (float)coverAngle };
		});
	}

	private void UpdateLayerReferences() {
		void InternalUpdateLayerReferences() {
			Root = FallbackRoot ?? LayerContainer;
            ProfilePage = LayerContainer?.GetLayer("res://scenes/menu/notebook/pages/profile.tscn") as Notebook.ProfilePage ?? FallbackProfilePage as Notebook.ProfilePage;
            ProfileViewport = LayerContainer?.GetViewport("res://scenes/menu/notebook/pages/profile.tscn");
            CoverPivot = LayerContainer?.GetPivot("res://scenes/menu/notebook/pages/cover.tscn") ?? FallbackCoverPivot;
			RestoreAnimationState();
        }
		// If LayerContainer exists, defer until it is ready.
		if (LayerContainer.IsValid()) LayerContainer.OnReady(InternalUpdateLayerReferences); else InternalUpdateLayerReferences();
	}

	public override void _Ready() {
		if (Engine.IsEditorHint()) return;

		Open = false; Highlighted = false;

		UpdateLayerReferences();
	}
	public override void _EnterTree() { RestoreAnimationState(); if (Engine.IsEditorHint()) CallableUtils.CallDeferred(UpdateLayerReferences); }
	public override void _ExitTree() {
		foreach (var (_, tween) in _tweens) if (tween.IsValid()) tween.Kill();
		_tweens.Clear();
		if (Engine.IsEditorHint()) { Root = null; ProfilePage = null; ProfileViewport = null; CoverPivot = null; }
	}
    protected virtual void PreScriptReload() { Root = null; ProfilePage = null; ProfileViewport = null; CoverPivot = null; }

	private void OnFocus() => Highlighted = true;
	private void OnUnfocus() => Highlighted = false;
	private void ToggleOpen() => Open = !Open;
	private void TogglePauseMenu() => InPauseMenu = !InPauseMenu;

	public void UpdateDialogueViewNode() {
		if (Engine.IsEditorHint() || string.IsNullOrEmpty(SuitorName) || ProfilePage?.DialogueView is not ProfileDialogueView dialogueView) return;
		dialogueView.ProfileNode = $"{Case.ToSnake(SuitorName)}__profile";
	}

    public (Node, Viewport) TryGainFocus(FocusDirection direction, Viewport fromViewport) => direction switch {
		FocusDirection.Up or FocusDirection.Right => (ProfilePage?.ProfileButton, ProfileViewport),
		_ => (null, null) 
	};

	public bool TryLoseFocus(FocusDirection direction, out bool popViewport) {
		popViewport = true;
		if (Open && direction.IsAnyOf(FocusDirection.Left, FocusDirection.Down)) return false;
		return true;
	}
}
