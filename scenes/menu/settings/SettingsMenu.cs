using Godot;
using DamselsGambit.Util;
using System;

namespace DamselsGambit;

public partial class SettingsMenu : Control, IFocusContext, IBackContext
{
	[Export] private TabContainer TabContainer { get; set; }
	[Export] private BaseButton ExitButton { get; set; }

	public event Action OnExit;

	public override void _EnterTree() { ExitButton?.TryConnect(BaseButton.SignalName.Pressed, OnExitPressed); }
	public override void _ExitTree() { ExitButton?.TryDisconnect(BaseButton.SignalName.Pressed, OnExitPressed); }

	// Connected to Exit button Pressed signal
	private void OnExitPressed() => OnExit?.Invoke();
	
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
