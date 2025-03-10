using System;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using Bridge;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public sealed partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public static Control MainMenu { get; private set; }
	public static PauseMenu PauseMenu { get; private set; }
	public static Node DialogueInterface { get; private set; }
	public static CardGameController CardGameController { get; private set; }

	public static event Action<Control> OnMainMenuInitialised, OnMainMenuFreed;
	public static event Action<PauseMenu> OnPauseMenuInitialised, OnPauseMenuFreed;
	public static event Action<CardGameController> OnCardGameControllerInitialised, OnCardGameControllerFreed;

	private CanvasLayer _cardGameCanvasLayer = new() { Layer = 20, Name = "CardGameLayer" };
	private CanvasLayer _menuCanvasLayer = new() { Layer = 25, Name = "MenuLayer" };
	
	private static readonly PackedScene _cardGameScene = ResourceLoader.Load<PackedScene>("res://scenes/card_game.tscn");
	private static readonly PackedScene _dialogueInterfaceScene = ResourceLoader.Load<PackedScene>("res://scenes/dialogue_interface.tscn");
	private static readonly PackedScene _mainMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/menu/main_menu.tscn");
	private static readonly PackedScene _pauseMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/menu/pause_menu.tscn");

	public override void _EnterTree() {
		if (Instance is not null) throw AutoloadException.For(this);
		Instance = this;
		void OnTreeReadyCallback() { OnTreeReady(); GetTree().Root.Ready -= OnTreeReadyCallback; } GetTree().Root.Ready += OnTreeReadyCallback;
	}
	private void OnTreeReady() {
		AddChild(_cardGameCanvasLayer); _cardGameCanvasLayer.Owner = this;
		AddChild(_menuCanvasLayer); _menuCanvasLayer.Owner = this;
		
		GetTree().Connect(SceneTree.SignalName.NodeAdded, Callable.From((Node node) => { if (node is PopupMenu popup) { popup.TransparentBg = true; } }));

		MainMenu = GetTree().Root.FindChildWhere<Control>(x => x.SceneFilePath.Equals(_mainMenuScene.ResourcePath));
		PauseMenu = GetTree().Root.FindChildWhere<PauseMenu>(x => x.SceneFilePath.Equals(_pauseMenuScene.ResourcePath));
		CardGameController = GetTree().Root.FindChildWhere<CardGameController>(x => x.SceneFilePath.Equals(_cardGameScene.ResourcePath));
		DialogueInterface = GetTree().Root.FindChildWhere<DialogueView>(x => x.SceneFilePath.Equals(_dialogueInterfaceScene.ResourcePath));
		if (DialogueInterface is null) { DialogueInterface = _dialogueInterfaceScene.Instantiate(); _menuCanvasLayer.AddChild(DialogueInterface); DialogueInterface.Owner = this; }

		var sceneRoot = GetTree().Root.GetChildren().LastOrDefault();
		if (sceneRoot is null || sceneRoot.SceneFilePath.Equals("res://scenes/main.tscn")) { InitialiseMainMenu(); }
		else if (sceneRoot == CardGameController) { CallDeferred(MethodName.InitialiseCardGame, true, true); }
	}

	public void InitialiseMainMenu(bool force = true) {
		if (MainMenu is not null) { if (!force) return; MainMenu.GetParent().RemoveChild(MainMenu); MainMenu.QueueFree(); OnMainMenuFreed?.Invoke(MainMenu); MainMenu = null; }
		if (PauseMenu is not null) { PauseMenu.GetParent().RemoveChild(PauseMenu); PauseMenu.QueueFree(); OnPauseMenuFreed?.Invoke(PauseMenu); PauseMenu = null; }
		if (CardGameController is not null) { CardGameController.GetParent().RemoveChild(CardGameController); CardGameController.QueueFree(); OnCardGameControllerFreed?.Invoke(CardGameController); CardGameController = null; }

		MainMenu = _mainMenuScene.Instantiate<Control>();
		_menuCanvasLayer.AddChild(MainMenu); MainMenu.Owner = _menuCanvasLayer;
		OnMainMenuInitialised?.Invoke(MainMenu);
	}
	
	public void InitialiseCardGame(bool force = true, bool skipIntro = false) {
		if (CardGameController is not null) { if (!force) return; CardGameController.GetParent().RemoveChild(CardGameController); CardGameController.QueueFree(); OnCardGameControllerFreed?.Invoke(CardGameController); CardGameController = null; }
		if (PauseMenu is not null) { PauseMenu.GetParent().RemoveChild(PauseMenu); PauseMenu.QueueFree(); OnPauseMenuFreed?.Invoke(PauseMenu); PauseMenu = null; }
		if (MainMenu is not null) { MainMenu.GetParent().RemoveChild(MainMenu); MainMenu.QueueFree(); OnMainMenuFreed?.Invoke(MainMenu); MainMenu = null; }
		
		DialogueManager.Instance.Reset();

		CardGameController = _cardGameScene.Instantiate<CardGameController>();
		CardGameController.SkipIntro = skipIntro;
		_cardGameCanvasLayer.AddChild(CardGameController); CardGameController.Owner = _cardGameCanvasLayer;
		OnCardGameControllerInitialised?.Invoke(CardGameController);

		PauseMenu = _pauseMenuScene.Instantiate<PauseMenu>();
		_menuCanvasLayer.AddChild(PauseMenu); PauseMenu.Owner = _menuCanvasLayer;
		PauseMenu.Hide();
		OnPauseMenuInitialised?.Invoke(PauseMenu);
	}
} 
