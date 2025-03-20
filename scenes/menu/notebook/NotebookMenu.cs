using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

[Tool]
public partial class NotebookMenu : Control, IFocusableContainer
{
	[Export] public string SuitorName { get; set { field = value; UpdateDialogueViewNode(); } }

	[ExportGroup("States")]

	[ExportSubgroup("Closed", "Closed")]
	[Export(PropertyHint.Range, "-90,90,0.001,radians")] public double ClosedCoverAngle { get; set { field = value; if (!Open) CoverPivot?.OnReady(x => x.Rotation = x.Rotation with { Y = (float)value }); } } = 0f;
	
	[ExportSubgroup("Highlight", "Highlight")]
	[Export] public Vector2 HighlightOffset { get; set; }
	[Export] private double HighlightDuration { get; set; } = 0.2;
	[Export(PropertyHint.Range, "-90,90,0.001,radians")] public double HighlightCoverAngle { get; set { field = value; if (Highlighted && !Open) CoverPivot?.OnReady(x => x.Rotation = x.Rotation with { Y = (float)value }); } } = 0f;
	
	[ExportSubgroup("Open", "Open")]
	[Export] public Vector2 OpenOffset { get; set; }
	[Export] private double OpenDuration { get; set; } = 0.3;
	[Export(PropertyHint.Range, "-90,90,0.001,radians")] public double OpenCoverAngle { get; set { field = value; if (Open) CoverPivot?.OnReady(x => x.Rotation = x.Rotation with { Y = (float)value }); } } = 0f;

	[ExportGroup("Nodes")]
	[Export] private Control Root { get; set; }
	[ExportSubgroup("Profile")]
	[Export] private Node DialogueView { get; set { field = value; UpdateDialogueViewNode(); } }
	[Export] private Button ProfileButton { get; set; }
	[ExportSubgroup("Cover")]
	[Export] private Node3D CoverPivot { get; set; }

	[ExportGroup("Debug", "Debug")]
	[Export] private bool DebugOpen { get => Open; set => Open = value; }
	[Export] private bool DebugHighlighted { get => Highlighted; set => Highlighted = value; }
	
	private Tween _moveTween, _rotateTween;
	private void TweenPosition(Vector2 to, double duration) => this.OnReady(() => { _moveTween?.Kill(); _moveTween = CreateTween(); _moveTween.TweenProperty(Root, "position", to, duration); });
	private void TweenRotation(double to, double duration) => this.OnReady(() => { _rotateTween?.Kill(); _rotateTween = CreateTween(); _rotateTween.TweenProperty(CoverPivot, "rotation:y", to, duration); });

	public bool Open {
		get; set {
			if (Open != value) {
				TweenPosition(value switch {true => OpenOffset, false when Highlighted => HighlightOffset, false => new Vector2()}, OpenDuration);
				TweenRotation(value switch {true => OpenCoverAngle, false when Highlighted => HighlightCoverAngle, false => ClosedCoverAngle}, OpenDuration);
			}
			field = value;
		}
	} = false;
	public bool Highlighted {
		get; set {
			if (Highlighted != value && !Open) {
				TweenPosition(value switch {true => HighlightOffset, false => new Vector2()}, HighlightDuration);
				TweenRotation(value switch {true => HighlightCoverAngle, false => ClosedCoverAngle}, HighlightDuration);
			}
			field = value;
		}
	} = false;

	public override void _Ready() {
		if (Engine.IsEditorHint()) return;

		Open = false; Highlighted = false;

		ProfileButton?.Connect(Control.SignalName.MouseEntered, new Callable(this, MethodName.TriggerHighlight));
		ProfileButton?.Connect(Control.SignalName.MouseExited, new Callable(this, MethodName.TriggerUnhighlight));
		ProfileButton?.Connect(Control.SignalName.FocusEntered, new Callable(this, MethodName.TriggerHighlight));
		ProfileButton?.Connect(Control.SignalName.FocusExited, new Callable(this, MethodName.TriggerUnhighlight));
		ProfileButton?.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.ToggleOpen));
	}

	private void TriggerHighlight() { Highlighted = true; }
	private void TriggerUnhighlight() { Highlighted = false; }
	private void ToggleOpen() { Open = !Open; }

	public void UpdateDialogueViewNode() {
		if (Engine.IsEditorHint() || !DialogueView.IsValid() || string.IsNullOrEmpty(SuitorName)) return;
		(DialogueView as ProfileDialogueView).ProfileNode = $"{Case.ToSnake(SuitorName)}__profile";
	}

    public Control TryGainFocus(InputManager.FocusDirection direction) => direction switch {
		InputManager.FocusDirection.Up or InputManager.FocusDirection.Right => ProfileButton,
		_ => null
	};

	public bool TryLoseFocus(InputManager.FocusDirection direction) => direction switch {
		InputManager.FocusDirection.Left or InputManager.FocusDirection.Down when Open => false,
		_ => true
	};
}
