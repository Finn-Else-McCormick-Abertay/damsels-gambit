using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private readonly Dictionary<string, List<CharacterDisplay>> _characterDisplays = [];
    private readonly Dictionary<string, List<PropDisplay>> _propDisplays = [];
    private readonly HashSet<Node> _dialogueViews = [];

    public static ReadOnlyCollection<DialogueView> DialogueViews => Instance?._dialogueViews?.Where(x => x is DialogueView)?.Select(x => x as DialogueView)?.ToList()?.AsReadOnly();

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
        Runner?.Stop();
        AnimationDialogueCommands.FlushCommandQueue();
        InitRunner();
        ReloadEnvironments();
        Knowledge.Reset();
    }

    private void InitRunner(bool force = true) {
        if (Runner is not null) { if (force) { RemoveChild(Runner); Runner.QueueFree(); Runner = null; } else return; }
        if (ProfileRunner is not null) { if (force) { RemoveChild(ProfileRunner); ProfileRunner.QueueFree(); ProfileRunner = null; } else return; }

        _yarnProject ??= ResourceLoader.Load<YarnProject>("res://assets/dialogue/DamselsGambit.yarnproject");

        if (_textLineProvider is null) { _textLineProvider = new TextLineProvider(); this.AddOwnedChild(_textLineProvider); }

        if (_variableStorage.IsValid() && force) { _variableStorage.QueueFree(); _variableStorage = null; }
        if (_variableStorage is null) { _variableStorage = new InMemoryVariableStorage(); this.AddOwnedChild(_variableStorage); }

        Runner = new DialogueRunner { Name = "DialogueRunner", yarnProject = _yarnProject, lineProvider = _textLineProvider, variableStorage = _variableStorage, startAutomatically = false }; this.AddOwnedChild(Runner);
        ProfileRunner = new DialogueRunner { Name = "ProfileRunner", yarnProject = _yarnProject, lineProvider = _textLineProvider, variableStorage = _variableStorage, startAutomatically = false }; this.AddOwnedChild(ProfileRunner);

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

    public static void Register(CharacterDisplay display) => Instance?._characterDisplays?.GetOrAdd(display.CharacterName, [])?.Add(display);
    public static void Deregister(CharacterDisplay display) => Instance?._characterDisplays?.GetValueOrDefault(display.CharacterName)?.Remove(display);

    public static void Register(PropDisplay display) => Instance?._propDisplays?.GetOrAdd(display.PropName, [])?.Add(display);
    public static void Deregister(PropDisplay display) => Instance?._propDisplays?.GetValueOrDefault(display.PropName)?.Remove(display);

    public static void Register<TView>(TView view) where TView : Node, DialogueViewBase { Instance?._dialogueViews?.Add(view); (view is ProfileDialogueView ? ProfileRunner : Runner)?.OnReady(x => x.dialogueViews.Add(view)); }
    public static void Deregister<TView>(TView view) where TView : Node, DialogueViewBase { Instance?._dialogueViews?.Remove(view); (view is ProfileDialogueView ? ProfileRunner : Runner)?.OnReady(x => x.dialogueViews.Remove(view)); }

    public static IEnumerable<string> GetCharacterNames() => Instance?._characterDisplays?.Where(x => x.Value.Count > 0)?.Select(x => x.Key);
    public static IEnumerable<string> GetPropNames() => Instance?._propDisplays?.Where(x => x.Value.Count > 0)?.Select(x => x.Key);
    public static IEnumerable<string> GetEnvironmentNames() => Instance?._environments?.Keys;

    public static ReadOnlyCollection<CharacterDisplay> GetCharacterDisplays(string characterName) => Instance?._characterDisplays?.GetValueOr(characterName, [])?.AsReadOnly();
    public static ReadOnlyCollection<PropDisplay> GetPropDisplays(string propName) => Instance?._propDisplays?.GetValueOr(propName, [])?.AsReadOnly();

    // These are either CanvasLayers or CanvasItems - have to do it this way as they both have 'Visible' fields but are not derived from a shared interface
    public static IEnumerable<Node> GetEnvironmentItems(string environmentName) => Instance?._environments?.GetValueOrDefault(environmentName)?.GetSelfAndChildren()?.Where(node => node is CanvasLayer || node is CanvasItem) ?? [];
    public static IEnumerable<CanvasLayer> GetEnvironmentLayers(string environmentName) => Instance?._environments?.GetValueOrDefault(environmentName)?.GetSelfAndChildren()?.Where(node => node is CanvasLayer)?.Select(x => x as CanvasLayer) ?? [];

    public static IEnumerable<Node> GetAllItems(string itemName) => GetEnvironmentItems(itemName)?.Concat(GetCharacterDisplays(itemName))?.Concat(GetPropDisplays(itemName)) ?? [];

    public static bool DialogueExists(string nodeName) => _yarnProject?.Program?.Nodes?.ContainsKey(nodeName ?? "") ?? false;

    public static DialogueResult Run(string nodeName, bool force = true, bool orErrorDialogue = true) {
        if (force) Runner.Stop(); else if (Runner.IsDialogueRunning) return new DialogueResult(nodeName, false, "Dialogue already running and force set to false.");
		if (DialogueExists(nodeName)) {
			Runner.StartDialogue(nodeName);
            return new DialogueResult(nodeName, true);
		}
        var errorMsg = $"No such node '{nodeName}'";
        Console.Warning(errorMsg);
        if (orErrorDialogue) {
            Runner.StartDialogue("error");
            return new DialogueResult("error", false, errorMsg);
        }
        return new DialogueResult(nodeName, false, errorMsg);
    }

    public static DialogueResult TryRun(string nodeName, bool force = true) => Run(nodeName, force, false);

    public static void OnComplete(Callable callable) {
        if (!Runner.IsDialogueRunning) callable.Call();
        else CallableUtils.CallDeferred(() => Runner.Connect(DialogueRunner.SignalName.onDialogueComplete, callable, (uint)ConnectFlags.OneShot));
    }
    public static void OnComplete(Action action) => OnComplete(Callable.From(action));
    
    public class DialogueResult
    {
        private readonly string _node;

        public bool Success { get; private set; }
        public string Error { get; private set; }

        public static implicit operator bool(DialogueResult result) => result.Success;
        
        internal DialogueResult(string node, bool success, string error = "") { _node = node; Success = success; Error = error; }

        public void AndThen(Callable callable) => OnComplete(callable);
        public void AndThen(Action action) => OnComplete(Callable.From(action));
    }
}