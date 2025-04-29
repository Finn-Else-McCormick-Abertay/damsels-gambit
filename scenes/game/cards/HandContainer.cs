using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bridge;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/cards_hand.svg")]
public partial class HandContainer : Container, IReloadableToolScript, IFocusableContainer
{
    [Export] public BoxContainer.AlignmentMode Alignment { get; set { field = value; QueueSort(); } } = BoxContainer.AlignmentMode.Center;
    [Export] public bool Fill { get; set { field = value; QueueSort(); } } = false;

    // Hand size. Doesn't limit the number of children which can be added, but does act as a minimum number of cards for it to treat as exisiting when calculating the layout,
    // which avoids the cards all changing position when the card count drops below this value.
    [Export] public int HandSize { get; set { field = value; QueueSort(); } } = -1;

    [ExportGroup("Input")]
    [Export] public int MaxSelected { get; set; } = 1;

    [ExportGroup("Animation", "Animation")]
    [Export] public double AnimationTimeAdd { get; set; } = 0.0;
    [Export] public double AnimationTimeReorder { get; set; } = 0.0;
    [Export] public double AnimationTimeHighlight { get; set; } = 0.0;

    [ExportGroup("Curves", "Curve")]
    [Export] public Curve CurveSeparation { get; set {
            CurveSeparation?.TryDisconnect(Resource.SignalName.Changed, this, MethodName.QueueSort);
            field = value; QueueSort();
            CurveSeparation?.TryConnect(Resource.SignalName.Changed, this, MethodName.QueueSort);
        }
    }
    [Export] public Curve CurveOffset { get; set {
            CurveOffset?.TryDisconnect(Resource.SignalName.Changed, this, MethodName.QueueSort);
            field = value; QueueSort();
            CurveOffset?.TryConnect(Resource.SignalName.Changed, this, MethodName.QueueSort);
        }
    }
    [Export] public Curve CurveRotation { get; set {
            CurveRotation?.TryDisconnect(Resource.SignalName.Changed, this, MethodName.QueueSort);
            field = value; QueueSort();
            CurveRotation?.TryConnect(Resource.SignalName.Changed, this, MethodName.QueueSort);
        }
    }

    public override bool _PropertyCanRevert(StringName property) {
        if (property == PropertyName.CurveSeparation) return true;
        if (property == PropertyName.CurveOffset) return true;
        if (property == PropertyName.CurveRotation) return true;
        return base._PropertyCanRevert(property);
    }
    public override Variant _PropertyGetRevert(StringName property) {
        if (property == PropertyName.CurveSeparation) {
            var curve = new Curve { MinValue = -50f, MaxValue = 50f };
            curve.AddPoint(new(0f, 5f));
            return curve;
        }
        if (property == PropertyName.CurveOffset) {
            var curve = new Curve { MinValue = -50f, MaxValue = 50f };
            curve.AddPoint(new(0f, 0f)); curve.AddPoint(new(0.5f, 0f)); curve.AddPoint(new(1f, 0f));
            return curve;
        }
        if (property == PropertyName.CurveRotation) {
            var curve = new Curve { MinValue = -90f, MaxValue = 90f };
            curve.AddPoint(new(0f, 0f)); curve.AddPoint(new(0.5f, 0f)); curve.AddPoint(new(1f, 0f));
            return curve;
        }
        return base._PropertyGetRevert(property);
    }

    public override void _Ready() {
        if (Engine.IsEditorHint()) return;
        InputManager.Actions.SelectAt.InnerObject.Connect(GUIDEAction.SignalName.Triggered, OnSelectAt);
        InputManager.Actions.Accept.InnerObject.Connect(GUIDEAction.SignalName.Triggered, OnAccept);
    }

    public override void _EnterTree() {
        this.TryConnectAll((Node.SignalName.ChildEnteredTree, this, MethodName.OnChildEnteredTree),(Node.SignalName.ChildExitingTree, this, MethodName.OnChildExitingTree));
        /*CurveSeparation?.TryConnect(Resource.SignalName.Changed, QueueSort);
        CurveOffset?.TryConnect(Resource.SignalName.Changed, QueueSort);
        CurveRotation?.TryConnect(Resource.SignalName.Changed, QueueSort);*/
    }
    public override void _ExitTree() {
        /*CurveSeparation?.TryDisconnect(Resource.SignalName.Changed, QueueSort);
        CurveOffset?.TryDisconnect(Resource.SignalName.Changed, QueueSort);
        CurveRotation?.TryDisconnect(Resource.SignalName.Changed, QueueSort);*/
    }

    private void OnChildEnteredTree(Node child) {
        if (!Engine.IsEditorHint()) {
            child.TryConnectAll(
                (Control.SignalName.MouseEntered, QueueSort), (Control.SignalName.MouseExited, QueueSort),
                (Control.SignalName.FocusEntered, QueueSort), (Control.SignalName.FocusExited, QueueSort)
            );
            _newChildren.Add(child);
        }
    }
    private void OnChildExitingTree(Node child) {
        if (!Engine.IsEditorHint()) {
            child.TryDisconnectAll(
                (Control.SignalName.MouseEntered, QueueSort), (Control.SignalName.MouseExited, QueueSort),
                (Control.SignalName.FocusEntered, QueueSort), (Control.SignalName.FocusExited, QueueSort)
            );
            _newChildren.Remove(child);
            _selectedCards.Remove(child);
            _prevHighlightedState.Remove(child);
            _prevSelectedState.Remove(child);
            _prevIndex.Remove(child);
        }
    }

    public Node GetNextFocus(FocusDirection direction, Node child) {
        var nextIndex = direction switch {
            FocusDirection.Left => child.GetIndex() - 1,
            FocusDirection.Right => child.GetIndex() + 1,
            _ => -1
        };
        return nextIndex >= 0 && nextIndex < GetChildCount() ? GetChild<Control>(nextIndex) : null;
    }

    public Node TryGainFocus(FocusDirection direction) =>
        direction switch {
            _ when GetChildCount() == 0 => null,
            FocusDirection.Down or FocusDirection.Left => GetChildren().Last(),
            FocusDirection.Right or _ => GetChildren().First()
        };

    // Connected to Accept action
    private void OnAccept() {
        if (Engine.IsEditorHint()) return;
        
        foreach (Control child in GetChildren().Cast<Control>()) {
            if (child.HasFocus()) {
                bool isSelected = _selectedCards.Contains(child);
                _prevSelectedState[child] = isSelected;
                if (isSelected) { _selectedCards.Remove(child); }
                else if (MaxSelected == -1 || _selectedCards.Count < MaxSelected) { _selectedCards.Add(child); }
                QueueSort();
            }
        }
    }

    // Connected to SelectAt action
    private void OnSelectAt() {
        if (Engine.IsEditorHint()) return;

        var position = GetGlobalMousePosition();//InputManager.Actions.SelectAt.ValueAxis2d; // Value I'm getting out of GUIDE is broken in some kind of way that I don't have time to figure out rn

        CardDisplay selectedCard = null;
        foreach (Control child in GetChildren().Cast<Control>()) {
            if (child is not CardDisplay card) continue;
            
            var positionLocal = child.GetGlobalTransform().AffineInverse() * position;
            if (card.CardRect.HasPoint(positionLocal)) { selectedCard = card; }
        }
        if (selectedCard is not null) {
            selectedCard.GrabFocus();
            bool isSelected = _selectedCards.Contains(selectedCard);
            _prevSelectedState[selectedCard] = isSelected;
            if (isSelected) { _selectedCards.Remove(selectedCard); }
            else if (MaxSelected == -1 || _selectedCards.Count < MaxSelected) { _selectedCards.Add(selectedCard); }
            QueueSort();
        }
    }

    // Get card children which have been selected
    public IEnumerable<CardDisplay> GetSelected() => _selectedCards.Select(x => x as CardDisplay).Where(x => x.IsValid());

    // Used by the sort logic to determine which cards are selected, which are highlighted, which animation length to use for a given movement, etc
    [NonSerialized] private readonly HashSet<Node> _newChildren = [];
    [NonSerialized] private readonly HashSet<Node> _selectedCards = [];
    [NonSerialized] private readonly Dictionary<Node, bool> _prevHighlightedState = [];
    [NonSerialized] private readonly Dictionary<Node, bool> _prevSelectedState = [];
    [NonSerialized] private readonly Dictionary<Node, int> _prevIndex = [];
    [NonSerialized] private readonly Dictionary<Node, Tween> _tweens = [];

    // Runs on recieving Container's SortChildren notification
    // Finds the positions and rotations for all children following the exported curves, then tweens them to those positions according to the exported 'time' properties
    private void OnSortChildren() {
        foreach (var (node, tween) in _tweens) {
            if (!IsAncestorOf(node)) { if (GodotObject.IsInstanceValid(tween)) tween.Kill(); _tweens.Remove(node); }
        }

        var childCount = GetChildCount();
        var imaginaryCards = Math.Max(0, HandSize - childCount) + GetChildren().Where(x => !(x as CanvasItem)?.IsVisibleInTree() ?? false).Count();

        var maxCardWidth = 0f;
        var theoreticalCardsTotalWidth = GetChildren().Index().Aggregate(0f,
            (float cardsWidth, (int index, Node child) arg) => {
                if (arg.child is not CardDisplay card || !card.Visible) { return cardsWidth; }
                var cardTextureAspect = card.Texture is null ? 0f : card.Texture.GetWidth() / (float)card.Texture.GetHeight();
                card.Size = new Vector2(cardTextureAspect * Size.Y, Size.Y);
                maxCardWidth = MathF.Max(maxCardWidth, card.Size.X);
                return cardsWidth + card.Size.X;
            });
        for (int i = childCount; i < HandSize; ++i) { theoreticalCardsTotalWidth += maxCardWidth; }

        var theoreticalSeparationTotalWidth = Enumerable.Range(0, Math.Max(childCount, HandSize)).Aggregate(0f, (x, i) => x + CurveSeparation?.Sample((i + 0.5f) / Math.Max(childCount, HandSize)) ?? 0f);

        var totalWidth = Fill ? Size.X : MathF.Min(Size.X, theoreticalCardsTotalWidth + theoreticalSeparationTotalWidth);
        
        // Something about this becomes slightly wrong with negative separation but I can't be fucked to figure it out rn
        var maxSeparation = totalWidth < Size.X ? float.PositiveInfinity : Size.X > maxCardWidth ? (childCount > 0 ? (Size.X - theoreticalCardsTotalWidth) / (childCount - 1) : 0f ) : -maxCardWidth;

        var startPoint = Alignment switch {
            BoxContainer.AlignmentMode.Center => (Size.X / 2f) - (Size.X > maxCardWidth ? totalWidth : maxCardWidth) / 2f,
            BoxContainer.AlignmentMode.Begin => 0,
            BoxContainer.AlignmentMode.End => Size.X - (Size.X > maxCardWidth ? totalWidth : maxCardWidth)
        };

        float runningTotal = startPoint;

        if (Alignment == BoxContainer.AlignmentMode.End && !Fill) { foreach (int i in RangeOf<int>.UpTo(imaginaryCards)) runningTotal += maxCardWidth; }

        foreach (var pair in GetChildren().Index()) {
            if (pair.Item is not Control card || !card.Visible) continue;
            var sampleIndex = pair.Index;
            if (Alignment == BoxContainer.AlignmentMode.End) sampleIndex += imaginaryCards;
            var samplePoint = (sampleIndex + 0.5f) / Math.Max(childCount, HandSize);
            if (pair.Index > 0) { runningTotal += Fill ? maxSeparation : MathF.Min(CurveSeparation?.Sample(samplePoint) ?? 0f, maxSeparation); }

            Vector2 newPosition = new(runningTotal, -CurveOffset?.Sample(samplePoint) ?? 0f);
            float newRotation = CurveRotation is not null ? CurveRotation.Sample(samplePoint) / 180 * MathF.PI : 0f;

            bool selected = _selectedCards.Contains(card);
            if (selected) { newPosition += new Vector2(0f, -40f); }
            
            _prevSelectedState.TryGetValue(card, out bool wasSelected);

            bool highlighted = ((card as CardDisplay)?.IsMousedOver ?? false) || card.HasFocus();
            if (highlighted && !Engine.IsEditorHint()) { newPosition += new Vector2(0f, -20f); }
            
            _prevHighlightedState.TryGetValue(card, out bool prevHighlighted);

            int prevIndex = _prevIndex.GetValueOr(card, -1);
            _prevIndex[card] = pair.Index;

            bool hasTween = _tweens.TryGetValue(card, out Tween oldTween);
            if (!(prevHighlighted && hasTween && oldTween.IsRunning())) { _prevHighlightedState[card] = highlighted; }
            if (!(wasSelected && hasTween && oldTween.IsRunning())) { _prevSelectedState[card] = selected; }

            bool skipAnimation = false;

            if (hasTween) {
                if (oldTween.IsRunning() && prevIndex == pair.Index) {
                    skipAnimation = true;
                    oldTween.TryConnect(Tween.SignalName.Finished, QueueSort);
                }
                else { oldTween.Kill(); _tweens.Remove(card); }
            }
            
            if (!skipAnimation) {
                var isNew = _newChildren.Contains(card); if (isNew) _newChildren.Remove(card);
                var animationTime = isNew ? AnimationTimeAdd : (highlighted != prevHighlighted || selected != wasSelected) ? AnimationTimeHighlight : AnimationTimeReorder;

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
            runningTotal += card.Size.X;
        }
    }
    
    protected void OnScriptReload() => QueueSort();
    public override void _Notification(int what) {
        switch ((long)what) {
            case NotificationSortChildren: OnSortChildren(); break;
        }
        base._Notification(what);
    }
}
