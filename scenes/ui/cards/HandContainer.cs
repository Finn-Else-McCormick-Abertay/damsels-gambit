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
        var availableSpacePerCard = Size.X / childCount;

        Vector2 cardSize = new(0f, Size.Y);
        if (childCount >= 0 && GetChild(0) is CardDisplay) {
            var card = GetChild(0) as CardDisplay;
            var cardTextureAspect = card.CardTexture.GetWidth() / (float)card.CardTexture.GetHeight();
            cardSize = cardSize with { X = cardTextureAspect * Size.Y };
        }

        var totalWidth = Fill ? Size.X : MathF.Min(Size.X, cardSize.X * childCount + Separation * (childCount - 1));
        var functionalSeparation = totalWidth < Size.X ? Separation : Size.X > cardSize.X ? (childCount > 0 ? (Size.X - cardSize.X * childCount) / (childCount - 1) : 0f ): -cardSize.X;

        var startPoint = Alignment switch {
            BoxContainer.AlignmentMode.Center => (Size.X / 2f) - (Size.X > cardSize.X ? totalWidth : cardSize.X) / 2f,
            BoxContainer.AlignmentMode.Begin => 0,
            BoxContainer.AlignmentMode.End => Size.X - (Size.X > cardSize.X ? totalWidth : cardSize.X),
            _ => throw new NotImplementedException()
        };
        foreach (var (i, child) in GetChildren().Cast<Control>().Index()) {
            child.Size = cardSize;
            child.Position = new(startPoint + (i * cardSize.X) + (i > 0 ? i * functionalSeparation : 0f), 0f);
        }
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
