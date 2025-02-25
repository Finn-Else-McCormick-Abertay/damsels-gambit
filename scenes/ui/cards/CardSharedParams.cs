using System;
using Godot;

namespace DamselsGambit;

[Tool]
public partial class CardSharedParams : Resource
{
	[Export(PropertyHint.Range, "0,0.5,")]
    public float CornerRadius { get; set { field = value; EmitChanged(); } } = 0.15f;

	[Export]
    public int CornerResolution { get; set { field = Math.Max(value, 1); EmitChanged(); } } = 10;

    [Export]
    public Vector2 NamePosition { get; set { field = new Vector2(MathF.Max(-0.5f, MathF.Min(0.5f, value.X)), MathF.Max(0f, MathF.Min(1f, value.Y))); EmitChanged(); } } = new(0f, 0.7f);

    [Export]
    public Vector2 ScorePosition { get; set { field = new Vector2(MathF.Max(-0.5f, MathF.Min(0.5f, value.X)), MathF.Max(0f, MathF.Min(1f, value.Y))); EmitChanged(); } } = new(0f, 0.945f);

    [Export]
    public Curve NameCurve {
        get;
        set {
            if (field is not null) { field.Changed -= EmitChanged; }
            field = value; EmitChanged();
            if (field is not null) { field.Changed += EmitChanged; }
        }
    }
}