using DamselsGambit.Dialogue;
using Godot;

namespace DamselsGambit;

[GlobalClass]
public partial class EnvironmentLayer : CanvasLayer
{
    [Export] public StringName EnvironmentName { get; set; }

    public override void _EnterTree() { DialogueManager.Register(this); Hide(); }

    public override void _ExitTree() => DialogueManager.Deregister(this);
}