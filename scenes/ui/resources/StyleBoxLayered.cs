using Godot;

namespace DamselsGambit;

[Tool, GlobalClass]
public partial class StyleBoxLayered : StyleBox
{
    [Export] public StyleBox[] StyleBoxes { get; set { field = value; EmitChanged(); } }

    public override void _Draw(Rid item, Rect2 rect) {
        foreach (var stylebox in StyleBoxes) {
            stylebox.Draw(item, rect);
        }
    }
}