using DamselsGambit;
using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

public partial class EndScreen : Control
{
	[Export] public Label MessageLabel { get; set; }
	[Export] Button RetryButton { get; set; }
	[Export] Button QuitButton { get; set; }

	public override void _Ready() {
		RetryButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnRetry));
		QuitButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnQuit));
		RetryButton?.GrabFocus();
	}

	private void OnRetry() { GameManager.SwitchToCardGameScene("res://scenes/dates/frostholm_date.tscn"); QueueFree(); }

	private static void OnQuit() { GameManager.SwitchToMainMenu(); }
}
