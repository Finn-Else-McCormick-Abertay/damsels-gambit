using DamselsGambit.Util;
using Godot;
using System;
using System.Linq;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/cards_hand.svg")]
public partial class HandContainer : Container, IReloadableToolScript
{
    [Export] public BoxContainer.AlignmentMode Alignment { get; set { field = value; QueueSort(); } } = BoxContainer.AlignmentMode.Center;
    [Export] public bool Fill { get; set { field = value; QueueSort(); } } = false;
    [Export] public float Separation { get; set { field = value; QueueSort(); } } = 5f;
    
    private void OnSortChildren() {
        var childCount = GetChildCount();

        var maxCardWidth = 0f;
        var theoreticalCardsTotalWidth = GetChildren().Index().Aggregate(0f,
            (float cardsWidth, (int index, Node child) arg) => {
                if (arg.child is not CardDisplay card || !card.Visible) { return cardsWidth; }
                var cardTextureAspect = card.Texture is null ? 0f : card.Texture.GetWidth() / (float)card.Texture.GetHeight();
                card.Size = new Vector2(cardTextureAspect * Size.Y, Size.Y);
                maxCardWidth = MathF.Max(maxCardWidth, card.Size.X);
                return cardsWidth + card.Size.X;
            });

        var totalWidth = Fill ? Size.X : MathF.Min(Size.X, theoreticalCardsTotalWidth + Separation * (childCount - 1));
        var functionalSeparation = totalWidth < Size.X ? Separation : Size.X > maxCardWidth ? (childCount > 0 ? (Size.X - theoreticalCardsTotalWidth) / (childCount - 1) : 0f ) : -maxCardWidth;

        var startPoint = Alignment switch {
            BoxContainer.AlignmentMode.Center => (Size.X / 2f) - (Size.X > maxCardWidth ? totalWidth : maxCardWidth) / 2f,
            BoxContainer.AlignmentMode.Begin => 0,
            BoxContainer.AlignmentMode.End => Size.X - (Size.X > maxCardWidth ? totalWidth : maxCardWidth),
            _ => throw new NotImplementedException()
        };

        var _ = GetChildren().Index().Aggregate(startPoint,
            (runningTotal, pair) => {
                if (pair.Item is not Control card || !card.Visible) { return runningTotal; }
                if (pair.Index > 0) { runningTotal += functionalSeparation; }
                card.Position = new(runningTotal, 0f);
                return runningTotal + card.Size.X;
            });
    }
    
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
}
