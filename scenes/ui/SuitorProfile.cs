using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

[Tool]
public partial class SuitorProfile : Control
{
    [Export] public string SuitorName { get; set { field = value; UpdateDialogueViewNode(); } }

    [ExportCategory("Offset")]
    [Export] public Vector2 HighlightOffset { get; set; }
    [Export] public Vector2 OpenOffset { get; set; }

    [Export] private double HighlightDuration { get; set; } = 0.2;
    [Export] private double OpenDuration { get; set; } = 0.3;

    [ExportGroup("Nodes")]
    [Export] private Node DialogueView { get; set { field = value; UpdateDialogueViewNode(); } }
    [Export] private Control Root { get; set; }
    [Export] private Button TabButton { get; set; }

    [ExportGroup("Debug", "Debug")]
    [Export] private bool DebugOpen { get => Open; set => Open = value; }
    [Export] private bool DebugHighlighted { get => Highlighted; set => Highlighted = value; }
    
    private Tween _moveTween;
    private bool Open {
        get; set {
            if (Open != value || (_moveTween?.IsRunning() ?? false)) {
                var offset = OpenOffset; if (Highlighted) { offset -= HighlightOffset; } offset *= value ? 1f : -1f;
                this.OnReady(() => { _moveTween?.Kill(); _moveTween = Root.CreateTween(); _moveTween.TweenProperty(Root, "position", value ? OpenOffset : Highlighted ? HighlightOffset : new Vector2(), OpenDuration); });
            }
            field = value;
        }
    } = false;
    private bool Highlighted {
        get; set {
            if ((Highlighted != value || (_moveTween?.IsRunning() ?? false)) && !Open) this.OnReady(() => {
                _moveTween?.Kill(); _moveTween = Root.CreateTween(); _moveTween.TweenProperty(Root, "position", value ? HighlightOffset : new Vector2(), HighlightDuration);
            });
            field = value;
        }
    } = false;

    public override void _Ready() {
        if (Engine.IsEditorHint()) return;

        Open = false; Highlighted = false;

        TabButton?.Connect(Control.SignalName.MouseEntered, new Callable(this, MethodName.TriggerHighlight));
        TabButton?.Connect(Control.SignalName.MouseExited, new Callable(this, MethodName.TriggerUnhighlight));
        TabButton?.Connect(Control.SignalName.FocusEntered, new Callable(this, MethodName.TriggerHighlight));
        TabButton?.Connect(Control.SignalName.FocusExited, new Callable(this, MethodName.TriggerUnhighlight));
        TabButton?.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.ToggleOpen));
    }

    private void TriggerHighlight() { Highlighted = true; }
    private void TriggerUnhighlight() { Highlighted = false; }
    private void ToggleOpen() { Open = !Open; }

    private void UpdateDialogueViewNode() {
        if (Engine.IsEditorHint() || !DialogueView.IsValid()) return;
        (DialogueView as ProfileDialogueView).ProfileNode = $"{Case.ToSnake(SuitorName)}/profile";
    }
}
