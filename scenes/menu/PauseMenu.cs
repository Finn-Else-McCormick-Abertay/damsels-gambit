using Bridge;
using DamselsGambit;
using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

public partial class PauseMenu : Control, IFocusContext, IBackContext
{
	[Export] Button ResumeButton { get; set; }
	[Export] Button QuitButton { get; set; }

	public override void _Ready() {
		Hide();
		ResumeButton.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnResume));
		QuitButton.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnQuit));

		InputManager.Actions.Pause.Connect(GUIDEAction.SignalName.Completed, new Callable(this, MethodName.TogglePaused), 0);
	}

	private void TogglePaused() {
		GetTree().Paused = !GetTree().Paused;
		Visible = GetTree().Paused;
		if (Visible) ResumeButton.GrabFocus();
	}

	private void OnResume() {
		GetTree().Paused = false;
		Visible = false;
	}

	private void OnQuit() {
		GetTree().Paused = false;
		GameManager.SwitchToMainMenu();
	}
	
    public virtual int FocusContextPriority => GetTree().Paused ? 5 : -1;
    public virtual int BackContextPriority => GetTree().Paused ? 5 : -1;

    public Control GetDefaultFocus() => ResumeButton;

	public virtual Control GetDefaultFocus(InputManager.FocusDirection direction) => direction switch {
		InputManager.FocusDirection.Up => QuitButton,
		InputManager.FocusDirection.Down or _ => ResumeButton
	};

    public bool UseBackInput() {
		if (GetTree().Paused) {
			OnResume();
			return true;
		}
		return false;
	}
}
