using DamselsGambit.Dialogue;
using Godot;
using System;

namespace DamselsGambit.Notebook;

public partial class ProfilePage : Control
{
	[Export] public ProfileDialogueView DialogueView { get; set; }
    [Export] public Button ProfileButton { get; set; }

    [Export] public Control MouseIndicator { get; set; }

    /*public override void _Input(InputEvent @event) {
        Console.Info("Profile page input: ", @event);
        if (@event is InputEventMouse mouseEvent) {
            if (MouseIndicator is not null) {
                MouseIndicator.Position = mouseEvent.Position;
            }
        }
    }*/

    public override void _GuiInput(InputEvent @event) {
        Console.Info("Profile page gui input: ", @event);
        if (@event is InputEventMouse mouseEvent) {
            if (MouseIndicator is not null) {
                MouseIndicator.Position = mouseEvent.Position;
            }
        }
    }
}
