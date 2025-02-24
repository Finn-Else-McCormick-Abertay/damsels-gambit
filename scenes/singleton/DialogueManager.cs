using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DamselsGambit.Util;
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
        Reset();
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

    private void ReloadEnvironments() {
        // Clear any existing environments
        // (animations can change the state of them, so we need to hard reset)
        _environments.Clear();
        foreach (var node in _environmentRoot.GetChildren()) { _environmentRoot.RemoveChild(node); node.QueueFree(); }
        foreach (var file in DirAccess.GetFilesAt("res://scenes/environment/")) {
            if (Path.GetExtension(file) != ".tscn") continue;
            var scene = ResourceLoader.Load<PackedScene>($"res://scenes/environment/{file}");
            if (scene is not null) {
                var node = scene.Instantiate();
                _environmentRoot.AddChild(node); node.Owner = _environmentRoot;

                var environmentName = Path.GetFileNameWithoutExtension(file);
                _environments.Add(environmentName, node);
                foreach (var item in GetEnvironmentItems(environmentName)) { item?.Set(CanvasItem.PropertyName.Visible, false); }
            }
        }
    }

    private readonly Node _environmentRoot = new() { Name = "EnvironmentRoot" };
    private readonly Dictionary<string, Node> _environments = [];
    private readonly Dictionary<string, CharacterDisplay> _characterDisplays = [];
    private readonly HashSet<DialogueView> _dialogueViews = [];

    public static void Register(CharacterDisplay display) => Instance?._characterDisplays?.Add(display.CharacterName, display);
    public static void Deregister(CharacterDisplay display) => Instance?._characterDisplays?.Remove(display.CharacterName);

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
    static class Commands
    {
        [YarnCommand("scene")]
        public static void Scene(string sceneName) {
            if (Instance is null) return;
            foreach (var (name, root) in Instance._environments) {
                bool visible = name == sceneName;
                foreach (var node in root.GetSelfAndChildren()) { if (node is CanvasLayer || node is CanvasItem) { node?.Set(CanvasItem.PropertyName.Visible, visible); } }
            }
        }

        [YarnCommand("show")]
        public static void Show(string itemName) {
            GetCharacterDisplay(itemName)?.Show();
            foreach (var item in GetEnvironmentItems(itemName)) { item?.Set(CanvasItem.PropertyName.Visible, true); }
        }

        [YarnCommand("hide")]
        public static void Hide(string itemName) {
            GetCharacterDisplay(itemName)?.Hide();
            foreach (var item in GetEnvironmentItems(itemName)) { item?.Set(CanvasItem.PropertyName.Visible, false); }
        }

        [YarnCommand("emote")]
        public static void Emote(string characterName, string emotionName) {
            var display = GetCharacterDisplay(characterName);
            if (display is null) return;
            if (display.Visible == false) { display.Show(); }
            display.SpriteName = emotionName;
        }

        [YarnCommand("move")]
        public static void Move(string characterName, float x, float y, float time = 0f) {
            var display = GetCharacterDisplay(characterName);
            if (display is null) return;
            if (time <= 0f) {
                display.Position += new Vector2(x, y);
                return;
            }
            var tween = display.CreateTween();
            tween.TweenProperty(display, Node2D.PropertyName.Position.ToString(), new Vector2(x, y), time).AsRelative();
        }

        [YarnCommand("fade")]
        public static void Fade(string inOut, string characterName, float time) {
            var display = GetCharacterDisplay(characterName);
            if (display is null) return;
            if (time <= 0f) {
                if (inOut == "in") { display.Show(); } else if (inOut == "out") { display.Hide(); }
                return;
            }

            float target;
            if (inOut == "in") { display.Modulate = display.Modulate with { A = 0f }; target = 1f; } else if (inOut == "out") { display.Modulate = display.Modulate with { A = 1f }; target = 0f; }
            else { return; }
            
            display.Show();

            var tween = display.CreateTween();
            tween.TweenProperty(display, "modulate:a", target, time);

            if (inOut == "out") { tween.TweenCallback(Callable.From(() => { display.Hide(); })); }
        }

        [YarnCommand("score")]
        public static void Score(int val) {
            GameManager.CardGameController.Score += val;
        }
    }
}