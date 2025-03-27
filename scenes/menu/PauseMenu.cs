/*
using Bridge;
using DamselsGambit;
using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

public partial class PauseMenu : Control, IFocusContext, IBackContext
{
	[Export] public Button ResumeButton { get; set; }
	[Export] public Button QuitButton { get; set; }

	public override void _Ready() {
		Hide();
		ResumeButton.TryConnect(BaseButton.SignalName.Pressed, OnResume);
		QuitButton.TryConnect(BaseButton.SignalName.Pressed, OnQuit);

		InputManager.Actions.Pause.InnerObject.Connect(GUIDEAction.SignalName.Completed, TogglePaused);
	}

	// Connected to Pause action
	private void TogglePaused() {
		GetTree().Paused = !GetTree().Paused;
		Visible = GetTree().Paused;
		if (Visible) ResumeButton.GrabFocus();
	}

	// Connected to Resume button Pressed signal
	private void OnResume() {
		GetTree().Paused = false;
		Visible = false;
	}

	// Connected to Quit button Pressed signal
	private void OnQuit() {
		GetTree().Paused = false;
		GameManager.SwitchToMainMenu();
	}
	
    public virtual int FocusContextPriority => GetTree().Paused ? 5 : -1;
    public virtual int BackContextPriority => GetTree().Paused ? 5 : -1;

    public Control GetDefaultFocus() => ResumeButton;

	public virtual Control GetDefaultFocus(FocusDirection direction) => direction switch {
		FocusDirection.Up => QuitButton,
		FocusDirection.Down or _ => ResumeButton
	};

	// Resume on back
    public bool UseBackInput() {
		if (GetTree().Paused) { OnResume(); return true; }
		return false;
	}
}
*/