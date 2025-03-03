using DamselsGambit;
using DamselsGambit.Util;
using Godot;
using System;

public partial class EndScreen : Control
{
	[Export] public Label MessageLabel { get; set; }
	[Export] Button RetryButton { get; set; }
	[Export] Button QuitButton { get; set; }

	public override void _Ready() {
		RetryButton?.TryConnect(Button.SignalName.Pressed, new Callable(this, MethodName.OnRetry));
		QuitButton?.TryConnect(Button.SignalName.Pressed, new Callable(this, MethodName.OnQuit));
		RetryButton?.GrabFocus();
	}

	private static void OnRetry() { GameManager.Instance.InitialiseCardGame(); }

	private static void OnQuit() { GameManager.Instance.InitialiseMainMenu(); }
}
