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
	
	private static readonly PackedScene _dialogueInterfaceScene = ResourceLoader.Load<PackedScene>("res://scenes/dialogue/dialogue_interface.tscn");
	private static readonly PackedScene _mainMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/menu/main_menu.tscn");
	private static readonly PackedScene _pauseMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/menu/pause_menu.tscn");

	public override void _EnterTree() {
		if (Instance is not null) throw AutoloadException.For(this);
		Instance = this;
		GetTree().Root.Connect(Node.SignalName.Ready, new Callable(this, MethodName.OnTreeReady), (uint)ConnectFlags.OneShot);
	}
	private void OnTreeReady() {
		AddChild(_cardGameCanvasLayer); _cardGameCanvasLayer.Owner = this;
		AddChild(_menuCanvasLayer); _menuCanvasLayer.Owner = this;
		
		GetTree().Connect(SceneTree.SignalName.NodeAdded, Callable.From((Node node) => { if (node is PopupMenu popup) { popup.TransparentBg = true; } }));

		MainMenu = GetTree().Root.FindChildWhere<Control>(x => x.SceneFilePath.Equals(_mainMenuScene.ResourcePath));
		if (MainMenu is not null) {
			_menuCanvasLayer.AddOwnedChild(MainMenu, true);
			OnMainMenuInitialised?.Invoke(MainMenu);
		}

		PauseMenu = GetTree().Root.FindChildWhere<PauseMenu>(x => x.SceneFilePath.Equals(_pauseMenuScene.ResourcePath));
		if (PauseMenu is not null) {
			_menuCanvasLayer.AddOwnedChild(PauseMenu, true);
			PauseMenu.Hide();
			OnPauseMenuInitialised?.Invoke(PauseMenu);
		}

		DialogueInterface = GetTree().Root.FindChildWhere<DialogueView>(x => x.SceneFilePath.Equals(_dialogueInterfaceScene.ResourcePath));
		if (DialogueInterface is null) { DialogueInterface = _dialogueInterfaceScene.Instantiate(); _menuCanvasLayer.AddOwnedChild(DialogueInterface); }
		
		CardGameController = GetTree().Root.FindChildOfType<CardGameController>();
		if (CardGameController is not null) {
			_cardGameCanvasLayer.AddOwnedChild(CardGameController, true);
			OnCardGameControllerInitialised?.Invoke(CardGameController);
			CardGameController.OnReady(x => x.CallDeferred(CardGameController.MethodName.BeginGame));
		}
	}

	public static void SwitchToMainMenu(bool force = true) {
		if (!Instance.IsValid()) return;

		if (MainMenu is not null) { if (!force) return; MainMenu.GetParent().RemoveChild(MainMenu); MainMenu.QueueFree(); OnMainMenuFreed?.Invoke(MainMenu); MainMenu = null; }
		if (PauseMenu is not null) { PauseMenu.GetParent().RemoveChild(PauseMenu); PauseMenu.QueueFree(); OnPauseMenuFreed?.Invoke(PauseMenu); PauseMenu = null; }
		if (CardGameController is not null) { CardGameController.GetParent().RemoveChild(CardGameController); CardGameController.QueueFree(); OnCardGameControllerFreed?.Invoke(CardGameController); CardGameController = null; }

		MainMenu = _mainMenuScene.Instantiate<Control>();
		Instance._menuCanvasLayer.AddOwnedChild(MainMenu);
		OnMainMenuInitialised?.Invoke(MainMenu);
	}

	public static void BeginGame() {
		DialogueManager.Instance.Reset();
		SwitchToCardGameScene("res://scenes/tutorial_date.tscn");
	}
	
	public static void SwitchToCardGameScene(string cardGameScenePath) {
		if (!Instance.IsValid()) return;
		
		if (CardGameController is not null) { CardGameController.GetParent().RemoveChild(CardGameController); CardGameController.QueueFree(); OnCardGameControllerFreed?.Invoke(CardGameController); CardGameController = null; }
		if (PauseMenu is not null) { PauseMenu.GetParent().RemoveChild(PauseMenu); PauseMenu.QueueFree(); OnPauseMenuFreed?.Invoke(PauseMenu); PauseMenu = null; }
		if (MainMenu is not null) { MainMenu.GetParent().RemoveChild(MainMenu); MainMenu.QueueFree(); OnMainMenuFreed?.Invoke(MainMenu); MainMenu = null; }

		var cardGameScene = ResourceLoader.Load<PackedScene>(cardGameScenePath).Instantiate();
		Instance._cardGameCanvasLayer.AddOwnedChild(cardGameScene);
		CardGameController = cardGameScene as CardGameController ?? cardGameScene.FindChildOfType<CardGameController>();
		OnCardGameControllerInitialised?.Invoke(CardGameController);
		CardGameController.OnReady(x => x.BeginGame());

		PauseMenu = _pauseMenuScene.Instantiate<PauseMenu>();
		Instance._menuCanvasLayer.AddOwnedChild(PauseMenu);
		PauseMenu.Hide();
		OnPauseMenuInitialised?.Invoke(PauseMenu);
	}
} 
