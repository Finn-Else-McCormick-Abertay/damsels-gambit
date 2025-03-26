using System;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit;

[GlobalClass, Tool]
public partial class CardTypeParams : Resource
{
    private static Vector2 InBounds(Vector2 val) => new(MathF.Max(-0.5f, MathF.Min(0.5f, val.X)), MathF.Max(0f, MathF.Min(1f, val.Y)));

    [Export] public Vector2 NamePosition { get; set { field = InBounds(value); EmitChanged(); } } = new(0f, 0.7f);
    [Export] public Vector2 TypePosition { get; set { field = InBounds(value); EmitChanged(); } } = new();
    [Export] public Vector2 ScorePosition { get; set { field = InBounds(value); EmitChanged(); } } = new(0f, 0.945f);

    [Export] public int NameFontSize { get; set { field = value; EmitChanged(); } } = -1;
    [Export] public int TypeFontSize { get; set { field = value; EmitChanged(); } } = -1;

    [Export]
    public Curve NameCurve {
        get;
        set {
            field?.TryDisconnect(Resource.SignalName.Changed, new Callable(this, Resource.MethodName.EmitChanged));
            field = value; EmitChanged();
            field?.TryConnect(Resource.SignalName.Changed, new Callable(this, Resource.MethodName.EmitChanged));
        }
    } = new();
}