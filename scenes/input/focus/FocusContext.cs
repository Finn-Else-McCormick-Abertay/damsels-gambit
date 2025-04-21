using Godot;

namespace DamselsGambit;

[GlobalClass]
public partial class FocusContext : Node, IFocusContext
{
    [Export] private int Priority { get; set; }

    [ExportGroup("Focus", "Focus")]
    [Export(PropertyHint.NodePathValidTypes, "Control")] public NodePath FocusDefault { get; set; }
    [Export(PropertyHint.NodePathValidTypes, "Control")] public NodePath FocusFromLeft { get; set; }
    [Export(PropertyHint.NodePathValidTypes, "Control")] public NodePath FocusFromRight { get; set; }
    [Export(PropertyHint.NodePathValidTypes, "Control")] public NodePath FocusFromTop { get; set; }
    [Export(PropertyHint.NodePathValidTypes, "Control")] public NodePath FocusFromBottom { get; set; }

    public virtual int FocusContextPriority => Priority;

    public virtual Control GetDefaultFocus() => GetNodeFor(FocusDefault);
    public virtual Control GetDefaultFocus(FocusDirection direction) => direction switch {
        // 'From right' when direction is left because focus moving leftwards means its moving in from our right, etc
        FocusDirection.Left => GetNodeFor(FocusFromRight) ?? GetNodeFor(FocusDefault), FocusDirection.Right => GetNodeFor(FocusFromLeft) ?? GetNodeFor(FocusDefault),
        FocusDirection.Up => GetNodeFor(FocusFromBottom) ?? GetNodeFor(FocusDefault), FocusDirection.Down => GetNodeFor(FocusFromTop) ?? GetNodeFor(FocusDefault),
        _ => GetNodeFor(FocusDefault)
    };

    private Control GetNodeFor(NodePath nodePath) {
        var taggedPath = new TaggedNodePath(nodePath,
            ("exists", arg => GetNode(arg) is Node node),
            ("visible", arg => GetNode(arg) is CanvasItem canvasItem && canvasItem.Visible)
        );
        return GetNodeOrNull<Control>(taggedPath.RootPath);
    }
}