using System;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using Bridge;
using System.Linq;
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

		var transitionLayer = AddLayer("transition", 99, false);
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

	private static void ClearLoadedScenesExcept(params IEnumerable<string> ignoreLayers) {
		if (!Instance.IsValid()) return;

		foreach (var (name, layer) in _layers) {
			if (!name.IsAnyOf(_resettableLayers) || name.IsAnyOf(ignoreLayers)) continue;
			//Console.Info($"Clearing layer '{name}'");
			foreach (var child in layer.GetChildren()) { layer.RemoveChild(child); child.QueueFree(); }
		}
		CardGameChanged?.Invoke();
	}

	private static void ClearLoadedScenes() => ClearLoadedScenesExcept();

	public static void SwitchToMainMenu() {
		if (!Instance.IsValid()) return;

		// Clearing other scenes is handled by the transition
		// Crossfade when coming from credits, otherwise fade to black
		SceneTransition.Run(GetLayer("credits").GetChildCount() > 0 ? SceneTransition.Type.CrossFade : SceneTransition.Type.FadeToBlack, "menu", () => {
			MainMenu = _mainMenuScene.Instantiate<Control>(); GetLayer("menu").AddChild(MainMenu);
			SetNotebookActive(false);
		});
	}
	
	public static void SwitchToCredits() {
		if (!Instance.IsValid()) return;

		// Clearing other scenes is handled by the transition
		SceneTransition.Run(SceneTransition.Type.CrossFade, "credits", () => {
			Credits = _creditsScene.Instantiate<Control>(); GetLayer("credits").AddChild(Credits);
			SetNotebookActive(false);
		});
	}

	public static void BeginGame() {
		DialogueManager.Instance.Reset();
		SwitchToCardGameScene("res://scenes/dates/tutorial_date.tscn");
	}
	
	public static void SwitchToCardGameScene(string cardGameScenePath) {
		if (!Instance.IsValid()) return;
		if (!ResourceLoader.Exists(cardGameScenePath)) { Console.Error($"No such scene '{cardGameScenePath}'"); return; }
		
		// Clearing other scenes is handled by the transition
		// Cut when coming from another card game scene, otherwise crossfade
		SceneTransition.Run(CardGameController.IsValid() ? SceneTransition.Type.Cut : SceneTransition.Type.CrossFade, "game", () => {
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
		public enum Type { Cut, FadeToBlack, CrossFade }

		private readonly Action _loadStep;
		private Type _type; private float? _duration; private string _fadeLayer;
		private Tween.TransitionType _interpolation = Tween.TransitionType.Quad;

		public float Duration => _duration switch { null or <0 => _type switch { Type.Cut => 0f, _ => 1.5f }, _ => (float)_duration };

		private SceneTransition(Action loadStep, Type type, float? duration = null, string fadeLayer = null) { _type = type; _loadStep = loadStep; _duration = duration; _fadeLayer = fadeLayer; }

		public static SceneTransition Create(Action loadStep) => new(loadStep, Type.Cut);
		public static SceneTransition Create(Type type, Action loadStep) => new(loadStep, type);
		public static SceneTransition Create(Type type, float duration, Action loadStep) => new(loadStep, type, duration);
		public static SceneTransition Create(Type type, string fadeLayer, float duration, Action loadStep) => new(loadStep, type, duration, fadeLayer);
		public static SceneTransition Create(Type type, string fadeLayer, Action loadStep) => new(loadStep, type, null, fadeLayer);
		
		public static SignalAwaiter Run(Action loadStep) => Create(loadStep).Run();
		public static SignalAwaiter Run(Type type, Action loadStep) => Create(type, loadStep).Run();
		public static SignalAwaiter Run(Type type, float duration, Action loadStep) => Create(type, duration, loadStep).Run();
		public static SignalAwaiter Run(Type type, string fadeLayer, float duration, Action loadStep) => Create(type, fadeLayer, duration, loadStep).Run();
		public static SignalAwaiter Run(Type type, string fadeLayer, Action loadStep) => Create(type, fadeLayer, loadStep).Run();

		public SceneTransition SetType(Type type) { _type = type; return this; }
		public SceneTransition SetDuration(float duration) { _duration = duration; return this; }
		public SceneTransition SetInterpolation(Tween.TransitionType interpolation) { _interpolation = interpolation; return this; }
		public SceneTransition SetFadeLayer(string name) { _fadeLayer = name; return this; }

		public SignalAwaiter Run() => Instance.ToSignal(_type switch {
			Type.Cut => PerformCut(),
			Type.FadeToBlack => PerformFadeToBlack(),
			Type.CrossFade => PerformCrossFade()
		}, Tween.SignalName.Finished);

		private Tween PerformCut() {
			var tween = Instance.CreateTween();
			var callbackTweener = tween.TweenCallback(() => { ClearLoadedScenes(); _loadStep?.Invoke(); foreach (var (name, layer) in _layers.Where(x => x.Key != "transition")) layer.Show(); });
			if (Duration > 0) callbackTweener.SetDelay(Duration);
			return tween;
		}

		private Tween PerformFadeToBlack() {
			var tween = FadeLayer("transition", FadeType.In, Duration / 2, _interpolation);
			tween.TweenCallback(() => { ClearLoadedScenes(); _loadStep?.Invoke(); foreach (var (name, layer) in _layers.Where(x => x.Key != "transition")) layer.Show(); });
			FadeLayer("transition", FadeType.Out, Duration / 2, _interpolation, tween);
			return tween;
		}

		private Tween PerformCrossFade() {
			_loadStep?.Invoke();
			var tween = Instance.CreateTween();
			if (_fadeLayer is null) return tween;

			var visibleLayers = _layers.Where(x => x.Value.IsValid() && x.Value.Visible && x.Value.GetChildCount() > 0 && _resettableLayers.Contains(x.Key));
			var sortedLayers = visibleLayers.Select(x => new KeyValuePair<string, float>(x.Key, x.Value.Layer)).OrderBy(x => x.Value).ToDictionary();
			var layerIndex = GetLayer(_fadeLayer).Layer;

			var layersAbove = sortedLayers.Where(x => x.Key != _fadeLayer).Where(x => x.Value >= layerIndex).Select(x => x.Key);

			Console.Info($"Layer: {_fadeLayer}({layerIndex}). Layers: {sortedLayers.ToPrettyString()}");

			if (layersAbove.Any()) {
				GetLayer(_fadeLayer).Show();
				foreach (var (index, layer) in layersAbove.Index()) { if (index != 0) tween.Parallel(); tween.TweenSubtween(FadeLayer(layer, FadeType.Out, Duration, _interpolation)); }
			}
			else FadeLayer(_fadeLayer, FadeType.In, Duration, _interpolation, tween);

			tween.TweenCallback(() => ClearLoadedScenesExcept(_fadeLayer));
			return tween;
		}

		private readonly Dictionary<CanvasItem, float> _cachedAlphaValues = [];

		private enum FadeType { In, Out }
		private Tween FadeLayer(string name, FadeType fade, float duration, Tween.TransitionType transitionType = Tween.TransitionType.Linear, Tween existingTween = null) {
			//Console.Info($"Fading {fade switch { FadeType.In => "in", FadeType.Out => "out" }} layer '{name}' over {duration}s with {Enum.GetName(transitionType).ToLower()} interpolation{existingTween switch { null => "", _ => $" using {existingTween.ToPrettyString()}" }}");
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
