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

    [Export] public Curve SeparationCurve { get; set { SeparationCurve?.TryDisconnect(Curve.SignalName.Changed, Callable.From(QueueSort)); field = value; QueueSort(); SeparationCurve?.TryConnect(Curve.SignalName.Changed, Callable.From(QueueSort)); } }
    [Export] public Curve OffsetCurve { get; set { OffsetCurve?.TryDisconnect(Curve.SignalName.Changed, Callable.From(QueueSort)); field = value; QueueSort(); OffsetCurve?.TryConnect(Curve.SignalName.Changed, Callable.From(QueueSort)); } }
    [Export] public Curve RotationCurve { get; set { RotationCurve?.TryDisconnect(Curve.SignalName.Changed, Callable.From(QueueSort)); field = value; QueueSort(); RotationCurve?.TryConnect(Curve.SignalName.Changed, Callable.From(QueueSort)); } }

    private static readonly Curve s_defaultSeparationCurve, s_defaultOffsetCurve, s_defaultRotationCurve;

    static HandContainer() {
        s_defaultSeparationCurve = new Curve { MinValue = -50f, MaxValue = 50f };
        s_defaultSeparationCurve.AddPoint(new(0f, 5f));

        s_defaultOffsetCurve = new Curve { MinValue = -50f, MaxValue = 50f };
        s_defaultOffsetCurve.AddPoint(new(0f, 0f)); s_defaultOffsetCurve.AddPoint(new(0.5f, 0f)); s_defaultOffsetCurve.AddPoint(new(1f, 0f));

        s_defaultRotationCurve = new Curve { MinValue = -90f, MaxValue = 90f };
        s_defaultRotationCurve.AddPoint(new(0f, 0f)); s_defaultRotationCurve.AddPoint(new(0.5f, 0f)); s_defaultRotationCurve.AddPoint(new(1f, 0f));
    }

    public override bool _PropertyCanRevert(StringName property) {
        if (property == PropertyName.SeparationCurve) return true;
        if (property == PropertyName.OffsetCurve) return true;
        if (property == PropertyName.RotationCurve) return true;
        return base._PropertyCanRevert(property);
    }
    public override Variant _PropertyGetRevert(StringName property) {
        if (property == PropertyName.SeparationCurve) return s_defaultSeparationCurve.Duplicate();
        if (property == PropertyName.OffsetCurve) return s_defaultOffsetCurve.Duplicate();
        if (property == PropertyName.RotationCurve) return s_defaultRotationCurve.Duplicate();
        return base._PropertyGetRevert(property);
    }

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

        var theoreticalSeparationTotalWidth = Enumerable.Range(0, childCount).Aggregate(0f, (x, i) => x + SeparationCurve?.Sample((i + 0.5f) / childCount) ?? 0f);

        var totalWidth = Fill ? Size.X : MathF.Min(Size.X, theoreticalCardsTotalWidth + theoreticalSeparationTotalWidth);
        
        // Something about this becomes slightly wrong with negative separation but I can't be fucked to figure it out rn
        var maxSeparation = totalWidth < Size.X ? float.PositiveInfinity : Size.X > maxCardWidth ? (childCount > 0 ? (Size.X - theoreticalCardsTotalWidth) / (childCount - 1) : 0f ) : -maxCardWidth;

        var startPoint = Alignment switch {
            BoxContainer.AlignmentMode.Center => (Size.X / 2f) - (Size.X > maxCardWidth ? totalWidth : maxCardWidth) / 2f,
            BoxContainer.AlignmentMode.Begin => 0,
            BoxContainer.AlignmentMode.End => Size.X - (Size.X > maxCardWidth ? totalWidth : maxCardWidth),
            _ => throw new NotImplementedException()
        };

        var _ = GetChildren().Index().Aggregate(startPoint,
            (runningTotal, pair) => {
                if (pair.Item is not Control card || !card.Visible) { return runningTotal; }
                var samplePoint = (pair.Index + 0.5f) / childCount;
                if (pair.Index > 0) { runningTotal += Fill ? maxSeparation : MathF.Min(SeparationCurve?.Sample(samplePoint) ?? 0f, maxSeparation); }
                card.Position = new(runningTotal, -OffsetCurve?.Sample(samplePoint) ?? 0f);
                card.Rotation = RotationCurve is not null ? RotationCurve.Sample(samplePoint) / 180 * MathF.PI : 0f;
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
