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

	public static event Action CardGameChanged;

	private CanvasLayer _cardGameCanvasLayer = new() { Layer = 20, Name = "CardGameLayer" };
	private CanvasLayer _dialogueCanvasLayer = new() { Layer = 23, Name = "DialogueLayer" };
	private CanvasLayer _menuCanvasLayer = new() { Layer = 25, Name = "MenuLayer" };
	
	private static readonly PackedScene _dialogueInterfaceScene = ResourceLoader.Load<PackedScene>("res://scenes/dialogue/dialogue_interface.tscn");
	private static readonly PackedScene _mainMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/main/main_menu.tscn");
	private static readonly PackedScene _pauseMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/pause_menu.tscn");

	public override void _EnterTree() {
		if (Instance is not null) throw AutoloadException.For(this);
		Instance = this;
		GetTree().Root.Connect(Node.SignalName.Ready, new Callable(this, MethodName.OnTreeReady), (uint)ConnectFlags.OneShot);
	}
	private void OnTreeReady() {
		this.AddOwnedChild(_cardGameCanvasLayer); this.AddOwnedChild(_dialogueCanvasLayer); this.AddOwnedChild(_menuCanvasLayer);
		
		GetTree().Connect(SceneTree.SignalName.NodeAdded, Callable.From((Node node) => { if (node is PopupMenu popup) { popup.TransparentBg = true; } }));

		MainMenu = GetTree().Root.FindChildWhere<Control>(x => x.SceneFilePath.Equals(_mainMenuScene.ResourcePath));
		if (MainMenu.IsValid()) _menuCanvasLayer.AddOwnedChild(MainMenu, true);

		PauseMenu = GetTree().Root.FindChildWhere<PauseMenu>(x => x.SceneFilePath.Equals(_pauseMenuScene.ResourcePath));
		if (PauseMenu.IsValid()) { _menuCanvasLayer.AddOwnedChild(PauseMenu, true); PauseMenu.Hide(); }

		DialogueInterface = GetTree().Root.FindChildWhere<DialogueView>(x => x.SceneFilePath.Equals(_dialogueInterfaceScene.ResourcePath)) ?? _dialogueInterfaceScene.Instantiate();
		if (DialogueInterface.IsValid()) _dialogueCanvasLayer.AddOwnedChild(DialogueInterface);
		
		CardGameController = GetTree().Root.FindChildOfType<CardGameController>();
		if (CardGameController.IsValid()) {
			var sceneRoot = GetTree().Root.GetChildren().Last();
			_cardGameCanvasLayer.AddOwnedChild(sceneRoot.IsAncestorOf(CardGameController) ? sceneRoot : CardGameController, true);
			CardGameController.OnReady(x => x.CallDeferred(CardGameController.MethodName.BeginGame, true));
			CardGameChanged?.Invoke();
		}
	}

	private static void ClearLoadedScenes() {
		if (!Instance.IsValid()) return;

		foreach (var child in Instance._menuCanvasLayer.GetChildren()) { Instance._menuCanvasLayer.RemoveChild(child); child.QueueFree(); }
		foreach (var child in Instance._cardGameCanvasLayer.GetChildren()) { Instance._cardGameCanvasLayer.RemoveChild(child); child.QueueFree(); }
		CardGameChanged?.Invoke();
	}

	public static void SwitchToMainMenu() {
		if (!Instance.IsValid()) return;

		InputManager.Instance.ClearFocus();
		ClearLoadedScenes();

		MainMenu = _mainMenuScene.Instantiate<Control>();
		Instance._menuCanvasLayer.AddOwnedChild(MainMenu);
	}

	public static void BeginGame() {
		DialogueManager.Instance.Reset();
		SwitchToCardGameScene("res://scenes/dates/tutorial_date.tscn");
	}
	
	public static void SwitchToCardGameScene(string cardGameScenePath) {
		if (!Instance.IsValid()) return;

		if (!ResourceLoader.Exists(cardGameScenePath)) {
			Console.Error($"No such scene '{cardGameScenePath}'");
			return;
		}

		InputManager.Instance.ClearFocus();
		ClearLoadedScenes();

		var cardGameScene = ResourceLoader.Load<PackedScene>(cardGameScenePath).Instantiate();
		Instance._cardGameCanvasLayer.AddOwnedChild(cardGameScene);
		CardGameController = cardGameScene as CardGameController ?? cardGameScene.FindChildOfType<CardGameController>();
		CardGameController.OnReady(x => x.BeginGame());
		CardGameChanged?.Invoke();

		PauseMenu = _pauseMenuScene.Instantiate<PauseMenu>();
		Instance._menuCanvasLayer.AddOwnedChild(PauseMenu);
		PauseMenu.Hide();
	}
} 
