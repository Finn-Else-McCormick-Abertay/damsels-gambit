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
        StartButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnStartPressed));
        SettingsButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnSettingsPressed));
        ExitButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnExitPressed));
    }
    public override void _ExitTree() {
        StartButton?.TryDisconnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnStartPressed));
        SettingsButton?.TryDisconnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnSettingsPressed));
        ExitButton?.TryDisconnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnExitPressed));
    }

    private void OnStartPressed() => GameManager.BeginGame();

    private void OnSettingsPressed() {
	    InputManager.PushToFocusStack();

        if (_settingsMenu.IsValid()) {
		    _settingsMenu.QueueFree();
		    _settingsMenu = null;
        }

        _settingsMenu = SettingsMenuScene?.Instantiate<SettingsMenu>();
        AddChild(_settingsMenu);

        _settingsMenu.OnExit += OnExitSettingsMenu;
        
        ButtonRoot.Hide();
    }

    private void OnExitSettingsMenu() {
        _settingsMenu.OnExit -= OnExitSettingsMenu;
        
        ButtonRoot.Show();
        
        _settingsMenu.Hide();
        _settingsMenu.QueueFree();
        _settingsMenu = null;
        
	    InputManager.PopFromFocusStack();
    }

    private void OnExitPressed() => GetTree().Quit();
    
    public virtual int FocusContextPriority => 8;

    public Control GetDefaultFocus() => StartButton;
    public Control GetDefaultFocus(FocusDirection direction) => direction switch {
        FocusDirection.Left => ExitButton,
        _ => StartButton
    };
}