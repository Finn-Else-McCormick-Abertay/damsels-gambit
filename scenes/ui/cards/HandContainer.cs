using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/cards_hand.svg")]
public partial class HandContainer : Container, IReloadableToolScript
{
    [Export] public BoxContainer.AlignmentMode Alignment { get; set { field = value; QueueSort(); } } = BoxContainer.AlignmentMode.Center;
    [Export] public bool Fill { get; set { field = value; QueueSort(); } } = false;

    [ExportGroup("Animation", "Animation")]
    [Export] public double AnimationTimeAdd { get; set; } = 0.0;
    [Export] public double AnimationTimeReorder { get; set; } = 0.0;
    [Export] public double AnimationTimeHighlight { get; set; } = 0.0;

    [ExportGroup("Curves", "Curve")]
    [Export] public Curve CurveSeparation { get; set { CurveSeparation?.TryDisconnect(Resource.SignalName.Changed, new Callable(this, MethodName.QueueSort)); field = value; QueueSort(); CurveSeparation?.TryConnect(Resource.SignalName.Changed, new Callable(this, MethodName.QueueSort)); } }
    [Export] public Curve CurveOffset { get; set { CurveOffset?.TryDisconnect(Resource.SignalName.Changed, new Callable(this, MethodName.QueueSort)); field = value; QueueSort(); CurveOffset?.TryConnect(Resource.SignalName.Changed, new Callable(this, MethodName.QueueSort)); } }
    [Export] public Curve CurveRotation { get; set { CurveRotation?.TryDisconnect(Resource.SignalName.Changed, new Callable(this, MethodName.QueueSort)); field = value; QueueSort(); CurveRotation?.TryConnect(Resource.SignalName.Changed, new Callable(this, MethodName.QueueSort)); } }

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
        if (property == PropertyName.CurveSeparation) return true;
        if (property == PropertyName.CurveOffset) return true;
        if (property == PropertyName.CurveRotation) return true;
        return base._PropertyCanRevert(property);
    }
    public override Variant _PropertyGetRevert(StringName property) {
        if (property == PropertyName.CurveSeparation) return s_defaultSeparationCurve.Duplicate();
        if (property == PropertyName.CurveOffset) return s_defaultOffsetCurve.Duplicate();
        if (property == PropertyName.CurveRotation) return s_defaultRotationCurve.Duplicate();
        return base._PropertyGetRevert(property);
    }

    public override void _EnterTree() {
        this.TryConnect(SignalName.ChildEnteredTree, new Callable(this, MethodName.OnChildEnteredTree));
        this.TryConnect(SignalName.ChildExitingTree, new Callable(this, MethodName.OnChildExitingTree));
    }
    private void OnChildEnteredTree(Node child) {
        if (!Engine.IsEditorHint()) {
            child.TryConnect(SignalName.MouseEntered, new Callable(this, MethodName.QueueSort));
            child.TryConnect(SignalName.MouseExited, new Callable(this, MethodName.QueueSort));
            _newChildren.Add(child);
        }
    }
    private void OnChildExitingTree(Node child) {
        if (!Engine.IsEditorHint()) {
            child.TryDisconnect(SignalName.MouseEntered, new Callable(this, MethodName.QueueSort));
            child.TryConnect(SignalName.MouseExited, new Callable(this, MethodName.QueueSort));
            _prevMouseOverState.Remove(child);
            _prevIndex.Remove(child);
        }
    }

    private readonly HashSet<Node> _newChildren = [];
    private readonly Dictionary<Node, bool> _prevMouseOverState = [];
    private readonly Dictionary<Node, int> _prevIndex = [];
    private readonly Dictionary<Node, Tween> _tweens = [];

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

        var theoreticalSeparationTotalWidth = Enumerable.Range(0, childCount).Aggregate(0f, (x, i) => x + CurveSeparation?.Sample((i + 0.5f) / childCount) ?? 0f);

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
                if (pair.Index > 0) { runningTotal += Fill ? maxSeparation : MathF.Min(CurveSeparation?.Sample(samplePoint) ?? 0f, maxSeparation); }

                Vector2 newPosition = new(runningTotal, -CurveOffset?.Sample(samplePoint) ?? 0f);
                float newRotation = CurveRotation is not null ? CurveRotation.Sample(samplePoint) / 180 * MathF.PI : 0f;

                bool mousedOver = (card as CardDisplay)?.IsMousedOver ?? false;
                if (mousedOver && !Engine.IsEditorHint()) { newPosition += new Vector2(0f, -20f); }
                
                _prevMouseOverState.TryGetValue(card, out bool prevMousedOver);

                _prevIndex.TryGetValueOr(card, out int prevIndex, -1);
                _prevIndex[card] = pair.Index;

                bool hasTween = _tweens.TryGetValue(card, out Tween oldTween);
                if (!(prevMousedOver && hasTween && oldTween.IsRunning())) { _prevMouseOverState[card] = mousedOver; }

                bool skipAnimation = false;

                if (hasTween) {
                    if (oldTween.IsRunning() && prevIndex == pair.Index) {
                        skipAnimation = true;
                        oldTween.TryConnect(Tween.SignalName.Finished, new Callable(this, MethodName.QueueSort));
                    }
                    else { oldTween.Kill(); _tweens.Remove(card); }
                }
                
                if (!skipAnimation) {
                    var isNew = _newChildren.Contains(card); if (isNew) { _newChildren.Remove(card); }
                    var animationTime = isNew ? AnimationTimeAdd : mousedOver != prevMousedOver ? AnimationTimeHighlight : AnimationTimeReorder;

                    if (animationTime > 0.0 && !Engine.IsEditorHint()) {
                        var tween = CreateTween().SetParallel(true);
                        tween.TweenProperty(card, "position", newPosition, animationTime).SetEase(Tween.EaseType.InOut);
                        tween.TweenProperty(card, "rotation", newRotation, animationTime).SetEase(Tween.EaseType.InOut);
                        _tweens.Add(card, tween);
                    }
                    else {
                        card.Position = newPosition;
                        card.Rotation = newRotation;
                    }
                }
                return runningTotal + card.Size.X;
            });
    }
    
    protected void OnScriptReload() => QueueSort();
    public override void _Notification(int what) {
        switch ((long)what) {
            case NotificationSortChildren: { OnSortChildren(); } break;
        }
        base._Notification(what);
    }
}
