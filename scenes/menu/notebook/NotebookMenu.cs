using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using System;

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

	[ExportGroup("Nodes")]
	[Export] private ViewportLayerContainer LayerContainer { get; set; }

	public Notebook.ProfilePage ProfilePage {
		get; private set {
			ProfilePage?.ProfileButton?.TryDisconnect(Control.SignalName.FocusEntered, new Callable(this, MethodName.OnFocus));
			ProfilePage?.ProfileButton?.TryDisconnect(Control.SignalName.FocusExited, new Callable(this, MethodName.OnUnfocus));
			ProfilePage?.ProfileButton?.TryDisconnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.ToggleOpen));
			field = value;
			ProfilePage?.OnReady(page => {
				UpdateDialogueViewNode();
				page.ProfileButton?.Connect(Control.SignalName.FocusEntered, new Callable(this, MethodName.OnFocus));
				page.ProfileButton?.Connect(Control.SignalName.FocusExited, new Callable(this, MethodName.OnUnfocus));
				page.ProfileButton?.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.ToggleOpen));
			});
		}
	}
	public Viewport ProfileViewport { get; private set; }
	public Node3D CoverPivot { get; private set; }

	[ExportGroup("Debug", "Debug")]
	[Export] private AnimationState DebugState { get => State; set => State = value; }
	
	public enum AnimationState { Closed, Highlighted, Open };
	public AnimationState State { get; private set { AnimateStateChange(State, value); field = value; } }

	public bool Open { get; set { field = value; State = State switch { _ when Open => AnimationState.Open, AnimationState.Open when !Open && Highlighted => AnimationState.Highlighted, _ => AnimationState.Closed }; } } = false;
	public bool Highlighted { get; set { field = value; State = State switch { AnimationState.Open => AnimationState.Open, _ when Highlighted => AnimationState.Highlighted, _ => AnimationState.Closed }; } } = false;
	
	private Tween _moveTween, _rotateTween;
	private void AnimateStateChange(AnimationState oldState, AnimationState newState) {
		if (oldState == newState) return;

		double duration = newState switch {
			AnimationState.Open => OpenDuration,
			AnimationState.Highlighted => oldState switch { AnimationState.Open => OpenDuration, AnimationState.Closed => HighlightDuration, _ => 0 },
			AnimationState.Closed => oldState switch { AnimationState.Open => OpenDuration, AnimationState.Highlighted => HighlightDuration, _ => 0 }
		};

		var position = newState switch { AnimationState.Open => OpenOffset, AnimationState.Highlighted => HighlightOffset, AnimationState.Closed => ClosedOffset };
		var angle = newState switch { AnimationState.Open => OpenCoverAngle, AnimationState.Highlighted => HighlightCoverAngle, AnimationState.Closed => ClosedCoverAngle };

		this.OnReady(() => { _moveTween?.Kill(); _moveTween = CreateTween(); _moveTween.TweenProperty(LayerContainer, "position", position, duration); });

		this.OnReady(() => {
			_rotateTween?.Kill();
			if (CoverPivot.IsValid()) { _rotateTween = CreateTween(); _rotateTween.TweenProperty(CoverPivot, "rotation:y", angle, duration); }
		});
	}

	private void RestoreAnimationState() {
		if (!IsNodeReady()) return;
		
		var position = State switch { AnimationState.Open => OpenOffset, AnimationState.Highlighted => HighlightOffset, AnimationState.Closed => ClosedOffset };
		var angle = State switch { AnimationState.Open => OpenCoverAngle, AnimationState.Highlighted => HighlightCoverAngle, AnimationState.Closed => ClosedCoverAngle };
		LayerContainer?.OnReady(() => CallableUtils.CallDeferred(() => {
			LayerContainer.Position = position;
			if (CoverPivot.IsValid()) { CoverPivot.Rotation = CoverPivot.Rotation with { Y = (float)angle }; }
		}));
	}

	public override void _Ready() {
		if (Engine.IsEditorHint()) return;

		Open = false; Highlighted = false;

		LayerContainer.OnReady(() => {
			ProfilePage = LayerContainer.GetLayer("res://scenes/menu/notebook/pages/profile.tscn") as Notebook.ProfilePage;
			ProfileViewport = LayerContainer.GetViewport("res://scenes/menu/notebook/pages/profile.tscn");
			CoverPivot = LayerContainer.GetPivot("res://scenes/menu/notebook/pages/cover.tscn");
		});
	}
	public override void _EnterTree() { RestoreAnimationState(); }
	public override void _ExitTree() {
		_moveTween?.Kill(); _moveTween = null;
		_rotateTween?.Kill(); _rotateTween = null;
	}

	private void OnFocus() => Highlighted = true;
	private void OnUnfocus() => Highlighted = false;
	private void ToggleOpen() => Open = !Open;

	public void UpdateDialogueViewNode() {
		if (Engine.IsEditorHint() || string.IsNullOrEmpty(SuitorName) || ProfilePage?.DialogueView is not ProfileDialogueView dialogueView) return;
		dialogueView.ProfileNode = $"{Case.ToSnake(SuitorName)}__profile";
	}

    public (Node, Viewport) TryGainFocus(FocusDirection direction, Viewport fromViewport) => direction switch {
		FocusDirection.Up or FocusDirection.Right => (ProfilePage.ProfileButton, ProfileViewport),
		_ => (null, null)
	};

	public bool TryLoseFocus(FocusDirection direction, out bool popViewport) {
		popViewport = true;
		if (Open && direction.IsAnyOf(FocusDirection.Left, FocusDirection.Down)) return false;
		return true;
	}
}
