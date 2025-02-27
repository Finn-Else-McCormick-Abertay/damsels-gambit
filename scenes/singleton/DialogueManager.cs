using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DamselsGambit.Util;
using Eltons.ReflectionKit;
using Godot;
using YarnSpinnerGodot;

namespace DamselsGambit.Dialogue;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public partial class DialogueManager : Node
{
    public static DialogueManager Instance { get; private set; }
    public static DialogueRunner Runner { get; private set; }

    public override void _EnterTree() {
        Instance = this;
        AddChild(_environmentRoot); _environmentRoot.Owner = this;
        InitRunner();
        GetTree().Root.Ready += OnTreeReady;
    }
    private void OnTreeReady() {
        ReloadEnvironments(true);
        
        GetTree().Root.Ready -= OnTreeReady;
    }

    public void Reset() {
        if (Runner is not null) { RemoveChild(Runner); Runner.QueueFree(); Runner = null; }
        InitRunner();
        ReloadEnvironments();
    }

    private void InitRunner() {
        if (Runner is not null) return;
        Runner = new DialogueRunner { Name = "DialogueRunner", yarnProject = ResourceLoader.Load<YarnProject>("res://assets/dialogue/DamselsGambit.yarnproject"), startAutomatically = false };
        AddChild(Runner); Runner.Owner = this;
        Runner.SetDialogueViews(_dialogueViews);
        Runner.Ready += () => { Runner.SetDialogueViews(_dialogueViews); };
    }

    private void ReloadEnvironments(bool cleanupExisting = false) {
        // Clear any existing environments
        // (animations can change the state of them, so we need to hard reset)
        _environments.Clear();
        foreach (var node in _environmentRoot.GetChildren()) { _environmentRoot.RemoveChild(node); node.QueueFree(); }

        var rootFolder = "res://scenes/environment/";

        void LoadFilesIn(string folderPath) {
            foreach (var rawPath in DirAccess.GetFilesAt(folderPath)) {
                var file = Path.GetExtension(rawPath) == ".remap" ? Path.GetFileNameWithoutExtension(rawPath) : rawPath;
                if (Path.GetExtension(file) != ".tscn") continue;

                var fullPath = $"{folderPath}{file}";
                var environmentName = $"{folderPath.StripFront(rootFolder)}{Path.GetFileNameWithoutExtension(file)}";

                bool shouldLoad = true;
                if (cleanupExisting) {
                    var instances = GetTree().Root.FindChildrenWhere(x => x.SceneFilePath == fullPath);
                    if (instances.Count > 1) foreach (var extraInstance in instances[1..]) extraInstance.QueueFree();

                    var instance = instances.FirstOrDefault();
                    shouldLoad = instance is null;
                    if (instance is not null) {
                        _environments.Add(environmentName, instance);
                        Callable.From(() => {
                            instance.GetParent().RemoveChild(instance);
                            _environmentRoot.AddChild(instance); instance.Owner = _environmentRoot;
                        }).CallDeferred();
                    }
                }
                if (shouldLoad) {
                    var scene = ResourceLoader.Load<PackedScene>(fullPath);
                    if (scene is not null) {
                        var node = scene.Instantiate();
                        _environmentRoot.AddChild(node); node.Owner = _environmentRoot;

                        _environments.Add(environmentName, node);
                        foreach (var item in GetEnvironmentItems(environmentName)) { item?.Set(CanvasItem.PropertyName.Visible, false); }
                    }
                }
            }
            foreach (var directory in DirAccess.GetDirectoriesAt(folderPath)) LoadFilesIn($"{folderPath}{directory}/");
        }

        LoadFilesIn(rootFolder);
    }

    private readonly Node _environmentRoot = new() { Name = "EnvironmentRoot" };
    private readonly Dictionary<string, Node> _environments = [];
    private readonly Dictionary<string, CharacterDisplay> _characterDisplays = [];
    private readonly HashSet<DialogueView> _dialogueViews = [];

    public static void Register(CharacterDisplay display) {
        if (Instance is null) return;
        if (Instance._characterDisplays.TryGetValue(display.CharacterName, out var existing)) {
            Instance._characterDisplays.Remove(display.CharacterName);
            Console.Warning($"Duplicate CharacterDisplay for '{display.CharacterName}' registered.");
        }
        Instance._characterDisplays.Add(display.CharacterName, display);
    }
    public static void Deregister(CharacterDisplay display) {
        if (Instance is null) return;
        if (Instance._characterDisplays.TryGetValue(display.CharacterName, out var existing) && existing == display) Instance._characterDisplays.Remove(display.CharacterName);
    }

    public static void Register(DialogueView view) { Instance?._dialogueViews?.Add(view); if (Runner?.IsNodeReady() ?? false) { Runner.dialogueViews.Add(view); } }
    public static void Deregister(DialogueView view) { Instance?._dialogueViews?.Remove(view); if (Runner?.IsNodeReady() ?? false) { Runner.dialogueViews.Remove(view); } }

    public static IEnumerable<string> GetCharacterNames() => Instance?._characterDisplays?.Keys;

    public static CharacterDisplay GetCharacterDisplay(string characterName) {
        if (Instance is null) return null;
        Instance._characterDisplays.TryGetValue(characterName, out var display);
        return display;
    }

    public static IEnumerable<string> GetEnvironmentNames() => Instance?._environments?.Keys;

    // These are either CanvasLayers or CanvasItems - have to do it this way as they both have 'Visible' fields but are not derived from a shared interface
    public static IEnumerable<Node> GetEnvironmentItems(string environmentName) {
        if (Instance is null) return [];
        Instance._environments.TryGetValue(environmentName, out var environment);
        if (environment is null) return [];

        var layers = new List<Node>();
        foreach (var node in environment.GetSelfAndChildren()) { if (node is CanvasLayer || node is CanvasItem) { layers.Add(node); } }
        return layers;
    }

    public static bool Run(string nodeName, bool force = false, bool orErrorDialogue = true) {
        if (force) { Runner.Stop(); } else if (Runner.IsDialogueRunning) { return false; }
		if (Runner.yarnProject.Program.Nodes.ContainsKey(nodeName)) {
			Runner.StartDialogue(nodeName);
            return true;
		}
        Console.Error($"No such node '{nodeName}'"); GD.PushWarning($"No such node '{nodeName}'");
        if (orErrorDialogue) { Runner.StartDialogue("error"); }
        return false;
    }
    
    // Dialogue commands

    private class CommandState
    {
        public readonly Queue<Timer> Timers = new();
    }
    private readonly CommandState _commandState = new();

    private static void RunCommandDeferred(Action action) {
        if (Instance is null) return;
        var actionSignature = action.Method.GetSignature(false);
        var timer = Instance._commandState.Timers.LastOrDefault();
        if (timer is not null) {
            //Console.Info($"Waiting on deferral timer {timer} before running dialogue command: {actionSignature}");
            var awaiter = timer.ToSignal(timer, Timer.SignalName.Timeout);
            Task.Factory.StartNew(async () => {
                await awaiter;
                //Console.Info($"Running dialogue command: {actionSignature}");
                Callable.From(action).CallDeferred();
            });
        }
        else action?.Invoke();
    }

    // Non-blocking wait
    [YarnCommand("after")]
    public static void After(float time) {
        if (Instance is null) return;
        var timer = new Timer() { OneShot = true, WaitTime = time };
        Instance.AddChild(timer);
        Instance._commandState.Timers.Enqueue(timer);
        timer.Timeout += OnDeferralTimerTimeout;
        //Console.Info($"Timer {timer} queued. {string.Join(", ", Instance._commandState.Timers.Index().Select(x => $"{x.Index}: {x.Item}"))}");
        if (Instance._commandState.Timers.Count <= 1) { timer.Start(); /*Console.Info($"Timer {timer} started.");*/  }
    }
    private static void OnDeferralTimerTimeout() {
        if (Instance is null) return;
        if (Instance._commandState.Timers.TryDequeue(out var oldTimer)) {
            //Console.Info($"Timer {oldTimer} timeout.");
            oldTimer.Timeout -= OnDeferralTimerTimeout;
            Instance.RemoveChild(oldTimer);
            oldTimer.QueueFree();
        }
        if (Instance._commandState.Timers.TryPeek(out var nextTimer)) { nextTimer.Start(); /*Console.Info($"Timer {nextTimer} started.");*/ }
    }

    [YarnCommand("flush_command_queue")]
    public static void FlushCommandQueue() {
        if (Instance is null) return;
        foreach (var timer in Instance._commandState.Timers) {
            timer.Timeout -= OnDeferralTimerTimeout;
            timer.Connect(Timer.SignalName.Timeout, Callable.From(timer.QueueFree), (uint)ConnectFlags.OneShot);
            timer.Start(0.01f);
        }
        Instance._commandState.Timers.Clear();
    }

    [YarnCommand("scene")]
    public static void Scene(string sceneName) {
        RunCommandDeferred(() => {
            foreach (var (name, root) in Instance?._environments) {
                bool visible = name == sceneName;
                foreach (var node in root.GetSelfAndChildren()) { if (node is CanvasLayer || node is CanvasItem) { node?.Set(CanvasItem.PropertyName.Visible, visible); } }
            }
        });
    }

    [YarnCommand("show")]
    public static void Show(string itemName) {
        RunCommandDeferred(() => {
            GetCharacterDisplay(itemName)?.Show();
            foreach (var item in GetEnvironmentItems(itemName)) { item?.Set(CanvasItem.PropertyName.Visible, true); }
        });
    }

    [YarnCommand("hide")]
    public static void Hide(string itemName) {
        RunCommandDeferred(() => {
            GetCharacterDisplay(itemName)?.Hide();
            foreach (var item in GetEnvironmentItems(itemName)) { item?.Set(CanvasItem.PropertyName.Visible, false); }
        });
    }

    [YarnCommand("emote")]
    public static void Emote(string characterName, string emotionName, string from = "", string revertFrom = "") {
        var display = GetCharacterDisplay(characterName);
        if (display is null) return;
        RunCommandDeferred(() => {
            if (display.Visible == false) { display.Show(); }
            if (from == "from" && !string.IsNullOrWhiteSpace(revertFrom) && display.SpriteName != revertFrom) return;
            display.SpriteName = emotionName;
        });
    }

    [YarnCommand("move")]
    public static void Move(string characterName, float x, float y, float time = 0f) {
        RunCommandDeferred(() => {
            var display = GetCharacterDisplay(characterName);
            if (display is null) return;
            if (time <= 0f) {
                display.Position += new Vector2(x, y);
                return;
            }
            var tween = display.CreateTween();
            tween.TweenProperty(display, Node2D.PropertyName.Position.ToString(), new Vector2(x, y), time).AsRelative();
        });
    }

    [YarnCommand("fade")]
    public static void Fade(string inOut, string itemName, float time) {
        RunCommandDeferred(() => {
            List<CanvasItem> affectedItems = [];
            List<CanvasLayer> affectedLayers = [];

            var display = GetCharacterDisplay(itemName); 
            var environmentItems = GetEnvironmentItems(itemName);

            if (display is not null) affectedItems.Add(display);
            affectedItems.AddRange(environmentItems.Where(x => x is CanvasItem).Select(x => x as CanvasItem));
            foreach (var layer in environmentItems.Where(x => x is CanvasLayer).Select(x => x as CanvasLayer)) {
                var affectedInLayer = layer.GetChildren().Where(x => x is CanvasItem).Select(x => x as CanvasItem);
                affectedItems.AddRange(affectedInLayer);
                if (affectedInLayer.Any()) affectedLayers.Add(layer);
            }

            if (time <= 0f) {
                foreach (var item in affectedItems) if (inOut == "in") item.Show(); else if (inOut == "out") item.Hide();
                foreach (var layer in affectedLayers) if (inOut == "in") layer.Show(); else if (inOut == "out") layer.Hide();
                return;
            }

            foreach (var item in affectedItems) {
                float target;
                if (inOut == "in") { item.Modulate = item.Modulate with { A = 0f }; target = 1f; } else if (inOut == "out") { item.Modulate = item.Modulate with { A = 1f }; target = 0f; }
                else continue;
                
                item.Show();

                var tween = item.CreateTween();
                tween.TweenProperty(item, "modulate:a", target, time);

                if (inOut == "out") { tween.TweenCallback(Callable.From(item.Hide)); }
            }
            
            foreach (var layer in affectedLayers) {
                layer.Show();
                if (inOut == "out") {
                    var tween = layer.CreateTween();
                    tween.TweenInterval(time);
                    tween.TweenCallback(Callable.From(layer.Hide));
                }
            }
        });
    }

    [YarnCommand("score")]
    public static void Score(int val) {
        GameManager.CardGameController.Score += val;
    }
}