using Godot;
using DamselsGambit.Util;

namespace DamselsGambit;

public partial class MainMenu : Control, IFocusContext
{
	[Export] private BaseButton StartButton { get; set; }
	[Export] private BaseButton SettingsButton { get; set; }
	[Export] private BaseButton ExitButton { get; set; }
	[Export] private Control ButtonRoot { get; set; }

	[Export] private PackedScene SettingsMenuScene { get; set; }

	private SettingsMenu _settingsMenu;

	public override void _EnterTree() {
		StartButton?.TryConnect(BaseButton.SignalName.Pressed, GameManager.BeginGame);
		SettingsButton?.TryConnect(BaseButton.SignalName.Pressed, GameManager.SwitchToSettings);
		ExitButton?.TryConnect(BaseButton.SignalName.Pressed, GameManager.Quit);
	}
	public override void _ExitTree() {
		StartButton?.TryDisconnect(BaseButton.SignalName.Pressed, GameManager.BeginGame);
		SettingsButton?.TryDisconnect(BaseButton.SignalName.Pressed, GameManager.SwitchToSettings);
		ExitButton?.TryDisconnect(BaseButton.SignalName.Pressed, GameManager.Quit);
	}
	
	public virtual int FocusContextPriority => 8;

	public Control GetDefaultFocus() => StartButton;
	public Control GetDefaultFocus(FocusDirection direction) => direction switch {
		FocusDirection.Left => ExitButton,
		_ => StartButton
	};
}
