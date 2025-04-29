using Godot;
using DamselsGambit.Util;
using System;

namespace DamselsGambit;

public partial class SettingsMenu : Control, IFocusContext, IBackContext
{
	[Export] private TabContainer TabContainer { get; set; }
	[Export] private BaseButton ExitButton { get; set; }
	[Export] private BaseButton SaveButton { get; set; }

	public event Action OnExit;

	public override void _EnterTree() {
		ExitButton?.TryConnect(BaseButton.SignalName.Pressed, OnExitPressed);
		SaveButton?.TryConnect(BaseButton.SignalName.Pressed, OnSavePressed);
	}
	public override void _ExitTree() {
		ExitButton?.TryDisconnect(BaseButton.SignalName.Pressed, OnExitPressed);
		SaveButton?.TryDisconnect(BaseButton.SignalName.Pressed, OnSavePressed);
	}

	// Connected to Exit button Pressed signal
	private void OnExitPressed() { SettingsManager.Save(); OnExit?.Invoke(); }

	// Connected to Save button Pressed signal
	private void OnSavePressed() {
		SettingsManager.Save();
	}
	
	public virtual int FocusContextPriority => 10;

	public Control GetDefaultFocus() => TabContainer.GetTabBar();
	public Control GetDefaultFocus(FocusDirection direction) => direction switch {
		FocusDirection.Up => ExitButton,
		_ => InputManager.FindFocusableWithin(TabContainer.GetTabBar(), direction)
	};

	public virtual int BackContextPriority => 10;
	public bool UseBackInput() {
		OnExit?.Invoke();
		return true;
	}
}
