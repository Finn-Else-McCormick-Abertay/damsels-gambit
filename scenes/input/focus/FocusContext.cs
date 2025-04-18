using Godot;

namespace DamselsGambit;

[GlobalClass]
public partial class FocusContext : Node, IFocusContext
{
    [Export] private int Priority { get; set; }

    [ExportGroup("Focus", "Focus")]
    [Export] public Control FocusDefault { get; set; }
    [Export] public Control FocusFromLeft { get; set; }
    [Export] public Control FocusFromRight { get; set; }
    [Export] public Control FocusFromTop { get; set; }
    [Export] public Control FocusFromBottom { get; set; }

    public virtual int FocusContextPriority => Priority;

    public virtual Control GetDefaultFocus() => FocusDefault;
    public virtual Control GetDefaultFocus(FocusDirection direction) => direction switch {
        // 'From right' when direction is left because focus moving leftwards means its moving in from our right, etc
        FocusDirection.Left => FocusFromRight, FocusDirection.Right => FocusFromLeft,
        FocusDirection.Up => FocusFromBottom, FocusDirection.Down => FocusFromTop,
        _ => FocusDefault
    };
}