using DamselsGambit;
using DamselsGambit.Util;
using Godot;
using System;

public partial class PauseMenu : Control
{
	[Export] Button ResumeButton { get; set; }
	[Export] Button QuitButton { get; set; }

	public override void _Ready() {
		Hide();
		ResumeButton.TryConnect(Button.SignalName.Pressed, new Callable(this, MethodName.OnResume));
		QuitButton.TryConnect(Button.SignalName.Pressed, new Callable(this, MethodName.OnQuit));
	}

	public override void _Process(double delta) {
		if (Input.IsActionJustPressed("pause")) {
			GetTree().Paused = !GetTree().Paused;
			Visible = GetTree().Paused;
		}
	}

	private void OnResume() {
		GetTree().Paused = !GetTree().Paused;
		Visible = GetTree().Paused;
	}

	private void OnQuit() {
		GameManager.Instance.QuitToTitle();
	}
}
