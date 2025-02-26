using System;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using Bridge;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public sealed partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public static CardGameController CardGameController { get; private set; }
    public static Control MainMenu { get; private set; }
    public static PauseMenu PauseMenu { get; private set; }

    private CanvasLayer _cardGameCanvasLayer = new() { Layer = 20, Name = "CardGameLayer" };
    private CanvasLayer _menuCanvasLayer = new() { Layer = 25, Name = "MenuLayer" };
    
    private PackedScene _cardGameScene = ResourceLoader.Load<PackedScene>("res://scenes/card_game.tscn");
    private PackedScene _mainMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/menu/main_menu.tscn");
    private PackedScene _pauseMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/menu/pause_menu.tscn");

    public override void _EnterTree() {
        Instance = this;
        GetTree().Root.Ready += OnTreeReady;
    }
    public override void _Ready() {
        AddChild(_cardGameCanvasLayer); _cardGameCanvasLayer.Owner = this;
        AddChild(_menuCanvasLayer); _menuCanvasLayer.Owner = this;

        InitialiseMainMenu();
    }
    private void OnTreeReady() {
        GUIDE.Initialise(GetTree().Root.GetNode("GUIDE"));
        GUIDE.Connect(GUIDE.SignalName.InputMappingsChanged, new Callable(this, MethodName.OnInputMappingsChanged));
        GetTree().Root.Ready -= OnTreeReady;
    }

    public void InitialiseMainMenu(bool force = true) {
        if (MainMenu is not null) { if (!force) return; MainMenu.GetParent().RemoveChild(MainMenu); MainMenu.QueueFree(); MainMenu = null; }
        PauseMenu?.QueueFree(); PauseMenu = null;
        CardGameController?.QueueFree(); CardGameController = null;

        MainMenu = _mainMenuScene.Instantiate<Control>();
        _menuCanvasLayer.AddChild(MainMenu); MainMenu.Owner = _menuCanvasLayer;
    }
    
    public void InitialiseCardGame(bool force = true) {
        if (CardGameController is not null) { if (!force) return; CardGameController.GetParent().RemoveChild(CardGameController); CardGameController.QueueFree(); CardGameController = null; }
        if (PauseMenu is not null) { PauseMenu.GetParent().RemoveChild(PauseMenu); PauseMenu.QueueFree(); PauseMenu = null; }
        MainMenu?.QueueFree(); MainMenu = null;
        
        DialogueManager.Instance.Reset();

        CardGameController = _cardGameScene.Instantiate<CardGameController>();
        _cardGameCanvasLayer.AddChild(CardGameController); CardGameController.Owner = _cardGameCanvasLayer;

        PauseMenu = _pauseMenuScene.Instantiate<PauseMenu>();
        _menuCanvasLayer.AddChild(PauseMenu); PauseMenu.Owner = _menuCanvasLayer;
        PauseMenu.Hide();
    }

    private bool _keyboardAndMouseContextEnabled = false;
    private bool _controllerContextEnabled = false;

    private void OnInputMappingsChanged() {
        _keyboardAndMouseContextEnabled = GUIDE.IsMappingContextEnabled(GUIDE.Contexts.KeyboardAndMouse);
        _controllerContextEnabled = GUIDE.IsMappingContextEnabled(GUIDE.Contexts.Controller);
    }

    public override void _Input(InputEvent @event) {
        if (!_keyboardAndMouseContextEnabled && (@event is InputEventKey || @event is InputEventMouse)) {
            GUIDE.EnableMappingContext(GUIDE.Contexts.KeyboardAndMouse, true);
        }
        if (!_controllerContextEnabled && (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)) {
            GUIDE.EnableMappingContext(GUIDE.Contexts.Controller, true);
        }
    }
} 