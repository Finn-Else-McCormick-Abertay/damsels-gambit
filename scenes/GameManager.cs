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
	private static readonly HashSet<string> _resettableLayers = [];
	
	private static CanvasLayer AddLayer(string name, CanvasLayer layer, bool canReset = true, bool force = false) {
		name = Case.ToSnake(name);
		if (_layers.TryGetValue(name, out var existingLayer)) { if (!force) return null; existingLayer.QueueFree(); _layers.Remove(name); _resettableLayers.Remove(name); }
		_layers.Add(name, layer); Instance.AddOwnedChild(layer, true);
		if (canReset) _resettableLayers.Add(name);
		return layer;
	}
	public static CanvasLayer AddLayer(string name, int layer, bool canReset = true, bool force = false) => AddLayer(name, new CanvasLayer() { Layer = layer, Name = Case.ToPascal($"{name.Trim()}_layer") }, canReset, force);

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
		var dialogueLayer = AddLayer("dialogue", GetTree().Root.FindChildWhere<CanvasLayer>(x => x.SceneFilePath.Equals(_dialogueLayerScene.ResourcePath)) ?? _dialogueLayerScene.Instantiate<CanvasLayer>(), false);
		var notebookLayer = AddLayer("notebook", GetTree().Root.FindChildWhere<CanvasLayer>(x => x.SceneFilePath.Equals(_notebookLayerScene.ResourcePath)) ?? _notebookLayerScene.Instantiate<CanvasLayer>(), false);

		var transitionLayer = AddLayer("transition", 200, false);
		transitionLayer.Hide();
		var blackRect = new ColorRect() { Color = Colors.Black }; transitionLayer.AddChild(blackRect);
		blackRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

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
			if (!name.IsAnyOf(_resettableLayers)) continue;
			foreach (var child in layer.GetChildren()) { layer.RemoveChild(child); child.QueueFree(); }
		}
		CardGameChanged?.Invoke();
	}

	public static void SwitchToMainMenu() {
		if (!Instance.IsValid()) return;

		SceneTransition.Run(SceneTransition.Type.FadeToBlack, () => {
			ClearLoadedScenes();

			MainMenu = _mainMenuScene.Instantiate<Control>();
			GetLayer("menu").AddChild(MainMenu);

			SetNotebookActive(false);
		});
	}
	
	public static void SwitchToCredits() {
		if (!Instance.IsValid()) return;

		ClearLoadedScenes();

		Credits = _creditsScene.Instantiate<Control>();
		GetLayer("credits").AddOwnedChild(Credits);

		SetNotebookActive(false);
	}

	public static void BeginGame() {
		DialogueManager.Instance.Reset();
		SwitchToCardGameScene("res://scenes/dates/tutorial_date.tscn");
	}
	
	public static void SwitchToCardGameScene(string cardGameScenePath) {
		if (!Instance.IsValid()) return;
		if (!ResourceLoader.Exists(cardGameScenePath)) { Console.Error($"No such scene '{cardGameScenePath}'"); return; }
		
		SceneTransition.Run(SceneTransition.Type.FadeToBlack, () => {
			ClearLoadedScenes();

			var cardGameScene = ResourceLoader.Load<PackedScene>(cardGameScenePath).Instantiate();
			GetLayer("game").AddChild(cardGameScene);
			CardGameController = cardGameScene as CardGameController ?? cardGameScene.FindChildOfType<CardGameController>();
			CardGameController.OnReady(x => x.BeginGame());
			CardGameChanged?.Invoke();

			SetNotebookActive(true);
		});
	}

	private static void SetNotebookActive(bool active) { NotebookMenu.CanPause = active; NotebookMenu.Open = false; NotebookMenu.InPauseMenu = false; }

	private class SceneTransition
	{
		public enum Type { Cut, FadeToBlack }

		private readonly Action _loadStep;
		private readonly Type _type;
		private readonly float _duration;

		private static readonly float DefaultDuration = 1.5f;
		private static readonly Tween.TransitionType DefaultInterpolation = Tween.TransitionType.Quad;

		private SceneTransition(Action loadStep, Type type, float duration) { _type = type; _loadStep = loadStep; _duration = duration; }

		public static SceneTransition Create(Action loadStep) => new(loadStep, Type.Cut, 0f);
		public static SceneTransition Create(Type type, Action loadStep) => new(loadStep, type, type switch { Type.Cut => 0f, _ => DefaultDuration });
		public static SceneTransition Create(Type type, float duration, Action loadStep) => new(loadStep, type, duration);
		
		public static SignalAwaiter Run(Action loadStep) => Create(loadStep).Run();
		public static SignalAwaiter Run(Type type, Action loadStep) => Create(type, loadStep).Run();
		public static SignalAwaiter Run(Type type, float duration, Action loadStep) => Create(type, duration, loadStep).Run();

		public SignalAwaiter Run() => Instance.ToSignal(_type switch {
			Type.Cut => PerformCut(),
			Type.FadeToBlack => PerformFadeToBlack()
		}, Tween.SignalName.Finished);

		private Tween PerformCut() {
			var tween = Instance.CreateTween();
			var callbackTweener = tween.TweenCallback(_loadStep);
			if (_duration > 0) callbackTweener.SetDelay(_duration);
			return tween;
		}

		private Tween PerformFadeToBlack() {
			var tween = FadeLayer("transition", FadeType.In, _duration / 2, DefaultInterpolation);
			tween.TweenCallback(_loadStep);
			FadeLayer("transition", FadeType.Out, _duration / 2, DefaultInterpolation, tween);
			return tween;
		}

		private readonly Dictionary<CanvasItem, float> _cachedAlphaValues = [];

		private enum FadeType { In, Out }
		private Tween FadeLayer(string name, FadeType fade, float duration, Tween.TransitionType transitionType = Tween.TransitionType.Linear, Tween existingTween = null) {
			var layer = GetLayer(name); var items = layer?.GetChildren()?.Where(x => x is CanvasItem)?.Cast<CanvasItem>() ?? [];

			if (fade == FadeType.In) CacheLayerAlphas(name);
			foreach (var item in items) item.Modulate = item.Modulate with { A = fade switch { FadeType.In => 0f, FadeType.Out => item.Modulate.A } };
			layer?.Show();
			
			var tween = existingTween ?? Instance.CreateTween();
			foreach (var (index, item) in items.Index()) {
				if (index != 0) tween.Parallel();
				tween.TweenProperty(item, "modulate:a", fade switch { FadeType.In => _cachedAlphaValues.GetValueOr(item, 1f), FadeType.Out => 0f }, duration).SetTrans(transitionType);
			}
			if (fade == FadeType.Out) tween.TweenCallback(() => { layer?.Hide(); foreach (var item in items) item.Modulate = item.Modulate with { A = _cachedAlphaValues.GetValueOr(item, 1f) }; });
			return tween;
		}

		private void CacheLayerAlphas(string name) { foreach (var item in GetLayer(name)?.GetChildren()?.Where(x => x is CanvasItem)?.Cast<CanvasItem>() ?? []) _cachedAlphaValues[item] = item.Modulate.A; }
	}
} 
