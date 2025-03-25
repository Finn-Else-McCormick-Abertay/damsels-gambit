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
	public override void _EnterTree() { if (Instance is not null) throw AutoloadException.For(this); Instance = this; GetTree().Root.Connect(Node.SignalName.Ready, OnTreeReady, (uint)ConnectFlags.OneShot); }
	
	public static CardGameController CardGameController { get; private set; }
	public static MainMenu MainMenu { get; private set; }
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

	// Get canvas layer by name
	public static CanvasLayer GetLayer(string name) => _layers.GetValueOrDefault(Case.ToSnake(name));
	// Set index of named canvas layer
	public static void SetLayer(string name, int layer) { var canvasLayer = GetLayer(name); if (canvasLayer.IsValid()) canvasLayer.Layer = layer; else Console.Error($"No such layer '{name}'"); }
	
	private static readonly PackedScene
		_dialogueLayerScene = ResourceLoader.Load<PackedScene>("res://scenes/dialogue/dialogue_layer.tscn"),
		_notebookLayerScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/notebook/notebook_layer.tscn"),
		_mainMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/main/main_menu.tscn"),
		_pauseMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/pause_menu.tscn");

	// Runs when full tree is ready
	private void OnTreeReady() {
		RenderingServer.SetDefaultClearColor(Colors.Black);
		GetTree().Connect(SceneTree.SignalName.NodeAdded, Callable.From((Node node) => { if (node is PopupMenu popup) { popup.TransparentBg = true; } }));

		// Create game, menu, dialogue and notebook canvasLayers
		var gameLayer = AddLayer("game", 20);
		var menuLayer = AddLayer("menu", 25);
		AddLayer("dialogue", GetTree().Root.FindChildWhere<CanvasLayer>(x => x.SceneFilePath.Equals(_dialogueLayerScene.ResourcePath)) ?? _dialogueLayerScene.Instantiate<CanvasLayer>());
		var notebookLayer = AddLayer("notebook", GetTree().Root.FindChildWhere<CanvasLayer>(x => x.SceneFilePath.Equals(_notebookLayerScene.ResourcePath)) ?? _notebookLayerScene.Instantiate<CanvasLayer>());

		// Find any existing instances of the scenes, and move them to the correct location if they exist (mainly for working correctly when loading into a place other than the default scene)

		NotebookMenu = notebookLayer.FindChildOfType<NotebookMenu>();

		MainMenu = GetTree().Root.FindChildWhere<MainMenu>(x => x.SceneFilePath.Equals(_mainMenuScene.ResourcePath));
		if (MainMenu.IsValid()) menuLayer.AddOwnedChild(MainMenu, true);

		PauseMenu = GetTree().Root.FindChildWhere<PauseMenu>(x => x.SceneFilePath.Equals(_pauseMenuScene.ResourcePath));
		if (PauseMenu.IsValid()) menuLayer.AddOwnedChild(MainMenu, true); PauseMenu?.Hide();
		
		CardGameController = GetTree().Root.FindChildOfType<CardGameController>();
		if (CardGameController.IsValid()) {
			var sceneRoot = GetTree().Root.GetChildren().Last();
			gameLayer.AddOwnedChild(sceneRoot.IsAncestorOf(CardGameController) ? sceneRoot : CardGameController, true);
			// If card game exists (because we are loading directly into a card game scene), immediately begin it
			CardGameController.OnReady(x => x.CallDeferred(CardGameController.MethodName.BeginGame, true));
			CardGameChanged?.Invoke();
		}
	}

	// Clear all loaded scenes (except those which always remain loaded)
	private static void ClearLoadedScenes() {
		if (!Instance.IsValid()) return;

		foreach (var (name, layer) in _layers) {
			if (name.IsAnyOf("dialogue", "notebook")) continue;
			foreach (var child in layer.GetChildren()) { layer.RemoveChild(child); child.QueueFree(); }
		}
		CardGameChanged?.Invoke();
	}

	// Load and switch to the main menu scene
	public static void SwitchToMainMenu() {
		if (!Instance.IsValid()) return;

		InputManager.ClearFocus();
		ClearLoadedScenes();

		MainMenu = _mainMenuScene.Instantiate<MainMenu>();
		GetLayer("menu").AddOwnedChild(MainMenu);
	}

	// Reset dialogue instance and switch to tutorial scene
	public static void BeginGame() {
		DialogueManager.Instance.Reset();
		SwitchToCardGameScene("res://scenes/dates/tutorial_date.tscn");
	}
	
	// Load and switch to card game scene at given path
	public static void SwitchToCardGameScene(string cardGameScenePath) {
		if (!Instance.IsValid()) return;

		if (!ResourceLoader.Exists(cardGameScenePath)) { Console.Error($"No such scene '{cardGameScenePath}'"); return; }

		InputManager.ClearFocus();
		ClearLoadedScenes();

		var cardGameScene = ResourceLoader.Load<PackedScene>(cardGameScenePath).Instantiate();
		GetLayer("game").AddOwnedChild(cardGameScene);
		CardGameController = cardGameScene as CardGameController ?? cardGameScene.FindChildOfType<CardGameController>();
		CardGameController.OnReady(BeginGame);
		CardGameChanged?.Invoke();

		PauseMenu = _pauseMenuScene.Instantiate<PauseMenu>();
		GetLayer("menu").AddOwnedChild(PauseMenu);
		PauseMenu.Hide();
	}
} 
