using Godot;
using System;
using DamselsGambit.Dialogue;

namespace DamselsGambit;

public partial class PropDisplay : Node2D
{
	[Export] public StringName PropName { get; set; }

	public override void _EnterTree() { if(!Engine.IsEditorHint()) { DialogueManager.Register(this); Hide(); } }
	public override void _ExitTree() { if(!Engine.IsEditorHint()) DialogueManager.Deregister(this); }
}
