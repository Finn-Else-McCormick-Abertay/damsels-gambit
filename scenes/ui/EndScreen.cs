using DamselsGambit;
using DamselsGambit.Util;
using Godot;
using System;

public partial class EndScreen : Control
{
	[Export] public Label MessageLabel { get; set; }
	[Export] Button QuitButton { get; set; }

	public override void _Ready() {
		QuitButton.TryConnect(Button.SignalName.Pressed, new Callable(GameManager.Instance, GameManager.MethodName.QuitToTitle));
	}
}
