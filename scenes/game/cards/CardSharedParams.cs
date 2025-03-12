using System;
using Godot;

namespace DamselsGambit;

[GlobalClass, Tool]
public partial class CardSharedParams : Resource
{
	[Export(PropertyHint.Range, "0,0.5,")]
    public float CornerRadius { get; set { field = value; EmitChanged(); } } = 0.03f;

	[Export]
    public int CornerResolution { get; set { field = Math.Max(value, 1); EmitChanged(); } } = 10;
}