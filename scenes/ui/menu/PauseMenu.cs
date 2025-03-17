using DamselsGambit;
using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

public partial class PauseMenu : Control, IFocusContext
{
	[Export] Button ResumeButton { get; set; }
	[Export] Button QuitButton { get; set; }

	public override void _Ready() {
		Hide();
		ResumeButton.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnResume));
		QuitButton.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnQuit));
	}

	public override void _Process(double delta) {
		if (Input.IsActionJustPressed("pause")) {
			GetTree().Paused = !GetTree().Paused;
			Visible = GetTree().Paused;
			if (Visible) ResumeButton.GrabFocus();
		}
	}

	private void OnResume() {
		GetTree().Paused = !GetTree().Paused;
		Visible = GetTree().Paused;
	}

	private void OnQuit() {
		GetTree().Paused = false;
		GameManager.SwitchToMainMenu();
	}
	
    public virtual int FocusContextPriority => GetTree().Paused ? 5 : -1;

    public Control GetDefaultFocus() => ResumeButton;

	public virtual Control GetDefaultFocus(InputManager.FocusDirection direction) => direction switch {
		InputManager.FocusDirection.Up => QuitButton,
		_ => ResumeButton
	};
}
