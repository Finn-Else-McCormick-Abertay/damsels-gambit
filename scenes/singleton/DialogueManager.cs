using System;
using System.Collections.Generic;
using System.Linq;
using DamselsGambit.Util;
using Godot;
using YarnSpinnerGodot;

namespace DamselsGambit.Dialogue;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public partial class DialogueManager : Node
{
    public static DialogueManager Instance { get; private set; }

    public static DialogueRunner Runner { get; private set; }
    public static DialogueRunner ProfileRunner { get; private set; }
    
    public static readonly Knowledge Knowledge = new();

    private static YarnProject _yarnProject;
    private static TextLineProvider _textLineProvider;
    private static VariableStorageBehaviour _variableStorage;
    
    private readonly Node _environmentRoot = new() { Name = "EnvironmentRoot" };
    private readonly Dictionary<string, Node> _environments = [];
    private readonly Dictionary<string, CharacterDisplay> _characterDisplays = [];
    private readonly HashSet<Node> _dialogueViews = [];

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
        InitRunner();
        ReloadEnvironments();
        Knowledge.Reset();
    }

    private void InitRunner(bool force = true) {
        if (Runner is not null) if (force) { RemoveChild(Runner); Runner.QueueFree(); Runner = null; } else return;
        if (ProfileRunner is not null) if (force) { RemoveChild(ProfileRunner); ProfileRunner.QueueFree(); ProfileRunner = null; } else return;

        _yarnProject ??= ResourceLoader.Load<YarnProject>("res://assets/dialogue/DamselsGambit.yarnproject");

        if (_textLineProvider is null) { _textLineProvider = new TextLineProvider(); AddChild(_textLineProvider); _textLineProvider.Owner = this; }
        if (_variableStorage is null) { _variableStorage = new InMemoryVariableStorage(); AddChild(_variableStorage); _variableStorage.Owner = this; }

        Runner = new DialogueRunner { Name = "DialogueRunner", yarnProject = _yarnProject, lineProvider = _textLineProvider, variableStorage = _variableStorage, startAutomatically = false }; AddChild(Runner); Runner.Owner = this;
        ProfileRunner = new DialogueRunner { Name = "ProfileRunner", yarnProject = _yarnProject, lineProvider = _textLineProvider, variableStorage = _variableStorage, startAutomatically = false }; AddChild(ProfileRunner); ProfileRunner.Owner = this;

        Runner.SetDialogueViews(_dialogueViews.Where(x => x is not ProfileDialogueView));
        ProfileRunner.SetDialogueViews(_dialogueViews.Where(x => x is ProfileDialogueView));
    }

    private void ReloadEnvironments(bool cleanupExisting = false) {
        // Clear any existing environments (animations can change the state of them, so we need to hard reset)
        _environments.Clear();
        foreach (var node in _environmentRoot.GetChildren()) { _environmentRoot.RemoveChild(node); node.QueueFree(); }

        foreach (var (fullPath, relativePath) in FileUtils.GetFilesOfTypeAbsoluteAndRelative<PackedScene>("res://scenes/environment/")) {
            var environmentName = relativePath.StripExtension();

            if (cleanupExisting) {
                var instances = GetTree().Root.FindChildrenWhere(x => x.SceneFilePath == fullPath);
                if (instances.Count > 1) foreach (var extraInstance in instances[1..]) extraInstance.QueueFree();

                var instance = instances.FirstOrDefault();
                if (instance is not null) {
                    _environments.Add(environmentName, instance);
                    Callable.From(() => {
                        instance.GetParent().RemoveChild(instance);
                        _environmentRoot.AddChild(instance); instance.Owner = _environmentRoot;
                    }).CallDeferred();
                    continue;
                }
            }
            var scene = ResourceLoader.Load<PackedScene>(fullPath);
            if (scene is not null) {
                var node = scene.Instantiate();
                _environmentRoot.AddChild(node); node.Owner = _environmentRoot;
                _environments.Add(environmentName, node);
                foreach (var item in GetEnvironmentItems(environmentName)) { item?.Set(CanvasItem.PropertyName.Visible, false); }
            }
        }
    }

    public static void Register(CharacterDisplay display) {
        if (Instance is null) return;
        if (Instance._characterDisplays.TryGetValue(display.CharacterName, out var _)) {
            Instance._characterDisplays.Remove(display.CharacterName);
            Console.Warning($"Duplicate CharacterDisplay for '{display.CharacterName}' registered.");
        }
        Instance._characterDisplays.Add(display.CharacterName, display);
    }
    public static void Deregister(CharacterDisplay display) {
        if (Instance is null) return;
        if (Instance._characterDisplays.TryGetValue(display.CharacterName, out var existing) && existing == display) Instance._characterDisplays.Remove(display.CharacterName);
    }

    public static void Register<TView>(TView view) where TView : Node, DialogueViewBase {
        Instance?._dialogueViews?.Add(view); (view is ProfileDialogueView ? ProfileRunner : Runner)?.OnReady(x => x.dialogueViews.Add(view));
    }
    public static void Deregister<TView>(TView view) where TView : Node, DialogueViewBase {
        Instance?._dialogueViews?.Remove(view); (view is ProfileDialogueView ? ProfileRunner : Runner)?.OnReady(x => x.dialogueViews.Remove(view));
    }

    public static IEnumerable<string> GetCharacterNames() => Instance?._characterDisplays?.Keys;

    public static CharacterDisplay GetCharacterDisplay(string characterName) {
        if (Instance is null) return null;
        Instance._characterDisplays.TryGetValue(characterName, out var display);
        return display;
    }

    public static IEnumerable<string> GetEnvironmentNames() => Instance?._environments?.Keys;

    // These are either CanvasLayers or CanvasItems - have to do it this way as they both have 'Visible' fields but are not derived from a shared interface
    public static IEnumerable<Node> GetEnvironmentItems(string environmentName) => Instance?._environments?.GetValueOrDefault(environmentName)?.GetSelfAndChildren()?.Where(node => node is CanvasLayer || node is CanvasItem) ?? [];

    public static bool DialogueExists(string nodeName) => _yarnProject?.Program?.Nodes?.ContainsKey(nodeName ?? "") ?? false;

    public static DialogueResult Run(string nodeName, bool force = true, bool orErrorDialogue = true) {
        if (force) Runner.Stop(); else if (Runner.IsDialogueRunning) return new DialogueResult(nodeName, false, "Dialogue already running and force set to false.");
		if (DialogueExists(nodeName)) {
			Runner.StartDialogue(nodeName);
            return new DialogueResult(nodeName, true);
		}
        var errorMsg = $"No such node '{nodeName}'";
        Console.Error(errorMsg);
        if (orErrorDialogue) {
            Runner.StartDialogue("error");
            return new DialogueResult("error", false, errorMsg);
        }
        return new DialogueResult(nodeName, false, errorMsg);
    }

    public static DialogueResult TryRun(string nodeName, bool force = true) => Run(nodeName, force, false);
    
    public class DialogueResult
    {
        private readonly string _node;

        public bool Success { get; private set; }
        public string Error { get; private set; }

        public static implicit operator bool(DialogueResult result) => result.Success;
        
        internal DialogueResult(string node, bool success, string error = "") { _node = node; Success = success; Error = error; }

        public void AndThen(Callable callable) {
            if (Runner.CurrentNodeName != _node || !Runner.IsDialogueRunning) { callable.Call(); return; }
            CallableUtils.CallDeferred(() => Runner.Connect(DialogueRunner.SignalName.onDialogueComplete, callable, (uint)ConnectFlags.OneShot));
        }
        public void AndThen(Action action) => AndThen(Callable.From(action));
    }
}