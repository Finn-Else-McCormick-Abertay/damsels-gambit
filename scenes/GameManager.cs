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
	public static Control Credits { get; private set; }
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
	public static void SetLayer(string name, int layer) { if (Engine.IsEditorHint()) return; var canvasLayer = GetLayer(name); if (canvasLayer.IsValid()) canvasLayer.Layer = layer; else Console.Error($"No such layer '{name}'"); }
	
	private static readonly PackedScene
		_dialogueLayerScene = ResourceLoader.Load<PackedScene>("res://scenes/dialogue/dialogue_layer.tscn"),
		_notebookLayerScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/notebook/notebook_layer.tscn"),
		_mainMenuScene = ResourceLoader.Load<PackedScene>("res://scenes/menu/main/main_menu.tscn"),
		_creditsScene = ResourceLoader.Load<PackedScene>("res://scenes/game/credits.tscn");
	
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
		var menuLayer = AddLayer("menu", 26);
		var creditsLayer = AddLayer("credits", 30);
		AddLayer("dialogue", GetTree().Root.FindChildWhere<CanvasLayer>(x => x.SceneFilePath.Equals(_dialogueLayerScene.ResourcePath)) ?? _dialogueLayerScene.Instantiate<CanvasLayer>());
		var notebookLayer = AddLayer("notebook", GetTree().Root.FindChildWhere<CanvasLayer>(x => x.SceneFilePath.Equals(_notebookLayerScene.ResourcePath)) ?? _notebookLayerScene.Instantiate<CanvasLayer>());

		NotebookMenu = notebookLayer.FindChildOfType<NotebookMenu>();

		MainMenu = GetTree().Root.FindChildWhere<Control>(x => x.SceneFilePath.Equals(_mainMenuScene.ResourcePath));
		if (MainMenu.IsValid()) menuLayer.AddOwnedChild(MainMenu, true);
		
		Credits = GetTree().Root.FindChildWhere<Control>(x => x.SceneFilePath.Equals(_creditsScene.ResourcePath));
		if (Credits.IsValid()) creditsLayer.AddOwnedChild(Credits, true);
		
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

		ClearLoadedScenes();

		MainMenu = _mainMenuScene.Instantiate<Control>();
		GetLayer("menu").AddOwnedChild(MainMenu);

		NotebookMenu.Open = false;
		NotebookMenu.InPauseMenu = false;
	}
	
	public static void SwitchToCredits() {
		if (!Instance.IsValid()) return;

		ClearLoadedScenes();

		Credits = _creditsScene.Instantiate<Control>();
		GetLayer("credits").AddOwnedChild(Credits);

		NotebookMenu.Open = false;
		NotebookMenu.InPauseMenu = false;
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

		ClearLoadedScenes();

		var cardGameScene = ResourceLoader.Load<PackedScene>(cardGameScenePath).Instantiate();
		GetLayer("game").AddOwnedChild(cardGameScene);
		CardGameController = cardGameScene as CardGameController ?? cardGameScene.FindChildOfType<CardGameController>();
		CardGameController.OnReady(x => x.BeginGame());
		CardGameChanged?.Invoke();

		NotebookMenu.Open = false;
		NotebookMenu.InPauseMenu = false;
	}
} 
