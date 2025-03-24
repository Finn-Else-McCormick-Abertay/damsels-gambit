using DamselsGambit.Dialogue;
using Godot;
using System;

namespace DamselsGambit.Notebook;

public partial class ProfilePage : Control
{
	[Export] public ProfileDialogueView DialogueView { get; set; }
    [Export] public Button ProfileButton { get; set; }
}
