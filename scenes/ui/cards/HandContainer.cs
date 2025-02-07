using DamselsGambit.Util;
using Godot;
using System;
using System.Linq;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/cards_hand.svg")]
public partial class HandContainer : Container, IReloadableToolScript
{
    [Export] public BoxContainer.AlignmentMode Alignment { get; set { field = value; QueueSort(); } } = BoxContainer.AlignmentMode.Center;
    [Export] public float MinCardSpace { get; set { field = value; QueueSort(); } } = 40f;

    protected void OnScriptReload() => QueueSort();
    public override void _Notification(int what) {
        switch ((long)what) {
            case NotificationLayoutDirectionChanged:
            case NotificationSortChildren: {
                OnSortChildren();
            } break;
        }
        base._Notification(what);
    }
    
    private void OnSortChildren() {
        var childCount = GetChildCount();
        var availableSpacePerCard = Size.X / childCount;
        var spacePerCard = Alignment == HorizontalAlignment.Fill ? availableSpacePerCard : MathF.Min(MinCardSpace, availableSpacePerCard);
        var startPoint = Alignment switch {
            HorizontalAlignment.Center => (Size.X / 2f) - (spacePerCard * childCount / 2f),
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Right => Size.X - (spacePerCard * childCount),
            HorizontalAlignment.Fill => 0,
            _ => throw new NotImplementedException()
        };
        foreach (var (i, child) in GetChildren().Cast<Control>().Index()) {
            child.Size = new(spacePerCard, Size.Y);
            child.Position = new(startPoint + (i * spacePerCard), 0f);
        }
    }

    public override Vector2 _GetMinimumSize() {
        return base._GetMinimumSize();
    }
}
