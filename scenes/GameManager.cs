using System;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using Bridge;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Generic;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public sealed partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }
	
	public static CardGameController CardGameController { get; private set; }

	public static Control MainMenu { get; private set; }
	public static PauseMenu PauseMenu { get; private set; }
	public static NotebookMenu NotebookMenu { get; private set; }

	public static event Action CardGameChanged;

	private static readonly Dictionary<string, CanvasLayer> _layers = [];
	
	private static CanvasLayer AddLayer(string name, CanvasLayer layer, bool force = false) {
		name = Case.ToSnake(name);
		if (_layers.TryGetValue(name, out var existingLayer)) { if (!force) return null; existingLayer.QueueFree(); _layers.Remove(name); }
		_layers.Add(name, layer); Instance.AddOwnedChild(layer, true);
		return layer;
	}
	public static CanvasLayer AddLayer(string name, int layer, bool force = false) => AddLayer(name, new CanvasLayer() { Layer = layer, Name = Case.ToPascal($"{name.Trim()}_layer") }, force);

	public static CanvasLayer GetLayer(string name) => _layers.GetValueOrDefault(Case.ToSnake(name));
	public static void SetLayer(string name, int layer) { var canvasLayer = GetLayer(name); if (canvasLayer.IsValid()) canvasLayer.Layer = layer; else Console.Error($"No such layer '{name}'"); }
	
	private static readonly PackedScene
		_dialogueLayerScene = ResourceLoader.Load<PackedScene>("res://scenes/dialogue/dialogue_layer.tscn"),
		_notebookLayerScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/notebook/notebook_layer.tscn"),
		_mainMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/main/main_menu.tscn"),
		_pauseMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/pause_menu.tscn");
	
	private static readonly Texture2D
		_cursorPointing = ResourceLoader.Load<Texture2D>("res://assets/ui/cursor/cursor_pointing_hand.png");

	public override void _EnterTree() {
		if (Instance is not null) throw AutoloadException.For(this);
		Instance = this;
		GetTree().Root.Connect(Node.SignalName.Ready, new Callable(this, MethodName.OnTreeReady), (uint)ConnectFlags.OneShot);
	}
	private void OnTreeReady() {
		RenderingServer.SetDefaultClearColor(Colors.Black);
		GetTree().Connect(SceneTree.SignalName.NodeAdded, Callable.From((Node node) => { if (node is PopupMenu popup) { popup.TransparentBg = true; } }));

		Input.SetCustomMouseCursor(_cursorPointing, Input.CursorShape.PointingHand);

		var gameLayer = AddLayer("game", 20);
		var menuLayer = AddLayer("menu", 25);
		AddLayer("dialogue", GetTree().Root.FindChildWhere<CanvasLayer>(x => x.SceneFilePath.Equals(_dialogueLayerScene.ResourcePath)) ?? _dialogueLayerScene.Instantiate<CanvasLayer>());
		var notebookLayer = AddLayer("notebook", GetTree().Root.FindChildWhere<CanvasLayer>(x => x.SceneFilePath.Equals(_notebookLayerScene.ResourcePath)) ?? _notebookLayerScene.Instantiate<CanvasLayer>());

		NotebookMenu = notebookLayer.FindChildOfType<NotebookMenu>();

		MainMenu = GetTree().Root.FindChildWhere<Control>(x => x.SceneFilePath.Equals(_mainMenuScene.ResourcePath));
		if (MainMenu.IsValid()) menuLayer.AddOwnedChild(MainMenu, true);

		PauseMenu = GetTree().Root.FindChildWhere<PauseMenu>(x => x.SceneFilePath.Equals(_pauseMenuScene.ResourcePath));
		if (PauseMenu.IsValid()) menuLayer.AddOwnedChild(MainMenu, true); PauseMenu?.Hide();
		
		CardGameController = GetTree().Root.FindChildOfType<CardGameController>();
		if (CardGameController.IsValid()) {
			var sceneRoot = GetTree().Root.GetChildren().Last();
			gameLayer.AddOwnedChild(sceneRoot.IsAncestorOf(CardGameController) ? sceneRoot : CardGameController, true);
			CardGameController.OnReady(x => x.CallDeferred(CardGameController.MethodName.BeginGame, true));
			CardGameChanged?.Invoke();
		}
	}

	private static void ClearLoadedScenes() {
		if (!Instance.IsValid()) return;

		foreach (var (name, layer) in _layers) {
			if (name.IsAnyOf([ "dialogue", "notebook" ])) continue;
			foreach (var child in layer.GetChildren()) { layer.RemoveChild(child); child.QueueFree(); }
		}
		CardGameChanged?.Invoke();
	}

	public static void SwitchToMainMenu() {
		if (!Instance.IsValid()) return;

		InputManager.Instance.ClearFocus();
		ClearLoadedScenes();

		MainMenu = _mainMenuScene.Instantiate<Control>();
		GetLayer("menu").AddOwnedChild(MainMenu);
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
		GetLayer("game").AddOwnedChild(cardGameScene);
		CardGameController = cardGameScene as CardGameController ?? cardGameScene.FindChildOfType<CardGameController>();
		CardGameController.OnReady(x => x.BeginGame());
		CardGameChanged?.Invoke();

		PauseMenu = _pauseMenuScene.Instantiate<PauseMenu>();
		GetLayer("menu").AddOwnedChild(PauseMenu);
		PauseMenu.Hide();
	}
} 
