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
	[Export] Button CreditsButton { get; set; }


	public override void _Ready() {
		RetryButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnRetry));
		QuitButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnQuit));
		CreditsButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnCredits));
		RetryButton?.GrabFocus();
		this.TryConnect(CanvasItem.SignalName.VisibilityChanged, new Callable(this, MethodName.OnVisibilityChanged));
		OnVisibilityChanged();
	}

	private void OnVisibilityChanged() {
		GameManager.NotebookMenu.Visible = !Visible;
	}

	private void OnRetry() {
		GameManager.NotebookMenu.Visible = true;
		GameManager.SwitchToCardGameScene("res://scenes/dates/frostholm_date.tscn");
		QueueFree();
	}

	private static void OnQuit() {
		GameManager.NotebookMenu.Visible = true;
		GameManager.SwitchToMainMenu();
	}

	private static void OnCredits() {
		GameManager.NotebookMenu.Visible = true;
		GameManager.SwitchToCredits();
	}
}
