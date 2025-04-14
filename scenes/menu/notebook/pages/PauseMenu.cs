using Godot;
using System;
using System.Collections.Generic;
using DamselsGambit.Util;

namespace DamselsGambit.Notebook;

public partial class PauseMenu : Control, IFocusContext, IFocusableContainer, IBackContext
{
    public bool Active {
        get;
        set {
            if (!field && value) { InputManager.PushToFocusStack(); InputManager.ClearFocus(); }
            if (field && !value) { InputManager.PopFromFocusStack(); }
            field = value;
            GetTree().Paused = Active;
            foreach (var button in new List<Button>() { ResumeButton, SettingsButton, QuitButton }) {
                button.FocusMode = Active ? FocusModeEnum.All : FocusModeEnum.None;
                button.MouseFilter = Active ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
            }
        }
    }

    [Export] public Button ResumeButton { get; set; }
    [Export] public Button SettingsButton { get; set; }
    [Export] public Button QuitButton { get; set; }
    
    [Export] private PackedScene SettingsMenuScene { get; set; }
    private SettingsMenu _settingsMenu;

    public bool InSettingsMenu => _settingsMenu.IsValid();

    public override void _Ready() {
        Active = false;
        SettingsButton?.Connect(BaseButton.SignalName.Pressed, OnSettingsPressed);
        QuitButton?.Connect(BaseButton.SignalName.Pressed, OnQuit);
    }

    private void OnSettingsPressed() {
	    InputManager.PushToFocusStack();

        if (_settingsMenu.IsValid()) { _settingsMenu.QueueFree(); _settingsMenu = null; }

        _settingsMenu = SettingsMenuScene?.Instantiate<SettingsMenu>();
        GameManager.GetLayer("menu").AddChild(_settingsMenu);
        _settingsMenu.ProcessMode = ProcessModeEnum.WhenPaused;

        _settingsMenu.OnExit += OnExitSettingsMenu;
    }

    private void OnExitSettingsMenu() {
        _settingsMenu.OnExit -= OnExitSettingsMenu;

        _settingsMenu.Hide();
        _settingsMenu.QueueFree();
        _settingsMenu = null;

	    InputManager.PopFromFocusStack();
    }

    private void OnQuit() {
        Active = false;
		GameManager.SwitchToMainMenu();
    }
    
    public virtual int FocusContextPriority => Active && !_settingsMenu.IsValid() ? 5 : -1;
    public virtual int BackContextPriority => Active && !_settingsMenu.IsValid() ? 5 : -1;

    public Control GetDefaultFocus() => ResumeButton;

	public virtual Control GetDefaultFocus(FocusDirection direction) => direction switch {
		FocusDirection.Up => QuitButton,
		FocusDirection.Down or _ => ResumeButton
	};

	// Resume on back
    public bool UseBackInput() {
		if (Active) { Active = false; return true; }
		return false;
	}
    
    public bool TryLoseFocus(FocusDirection direction, out bool popViewport) {
        popViewport = false;
        return false;
    }
}
