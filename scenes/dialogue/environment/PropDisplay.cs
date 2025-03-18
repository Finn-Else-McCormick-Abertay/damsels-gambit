using Godot;
using System;
using DamselsGambit.Dialogue;

namespace DamselsGambit.Environment;

public partial class PropDisplay : Node2D, IEnvironmentDisplay
{
	[Export] public StringName PropName { get; set; }
	
	private bool _initiallyVisible;

	public override void _EnterTree() { if(!Engine.IsEditorHint()) { EnvironmentManager.Register(this); _initiallyVisible = Visible; Hide(); } }
	public override void _ExitTree() { if(!Engine.IsEditorHint()) EnvironmentManager.Deregister(this); }
	
	public void RestoreInitialVisibility() => Visible = _initiallyVisible;
}
