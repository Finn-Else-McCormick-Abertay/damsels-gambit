using System.Collections.Generic;
using System.Threading.Tasks;
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
        InitRunner();
    }

    public void InitRunner() {
        if (Runner is not null) {
            RemoveChild(Runner);
            Runner.QueueFree();
        }
        Runner = new DialogueRunner {
            yarnProject = ResourceLoader.Load<YarnProject>("res://assets/dialogue/DamselsGambit.yarnproject"),
            startAutomatically = false
        };
        AddChild(Runner);
        Runner.SetDialogueViews(_dialogueViews);
        Runner.Ready += () => { Runner.SetDialogueViews(_dialogueViews); };
    }

    private readonly HashSet<CharacterDisplay> _characterDisplays = [];
    private readonly HashSet<EnvironmentLayer> _environmentLayers = [];
    private readonly HashSet<DialogueView> _dialogueViews = [];

    public static void Register(CharacterDisplay display) {
        if (Instance is null) return;
        Instance._characterDisplays.Add(display);
    }
    public static void Deregister(CharacterDisplay display) {
        if (Instance is null) return;
        Instance._characterDisplays.Remove(display);
    }
    
    public static void Register(EnvironmentLayer layer) {
        if (Instance is null) return;
        Instance._environmentLayers.Add(layer);
    }
    public static void Deregister(EnvironmentLayer layer) {
        if (Instance is null) return;
        Instance._environmentLayers.Remove(layer);
    }

    public static void Register(DialogueView view) {
        if (Instance is null) return;
        Instance._dialogueViews.Add(view);
        if (Runner is not null && Runner.IsNodeReady()) { Runner.dialogueViews.Add(view); }
    }
    public static void Deregister(DialogueView view) {
        if (Runner is null) return;
        Instance._dialogueViews.Remove(view);
        if (Runner is not null && Runner.IsNodeReady()) { Runner.dialogueViews.Remove(view); }
    }

    public static CharacterDisplay GetCharacterDisplay(string characterName) {
        if (Instance is null) return null;
        foreach (var display in Instance._characterDisplays) { if (display.CharacterName == characterName) return display; }
        return null;
    }

    public static IEnumerable<EnvironmentLayer> GetEnvironmentLayers(string environmentName) {
        if (Instance is null) return null;
        var layers = new List<EnvironmentLayer>();
        foreach (var layer in Instance._environmentLayers) { if (layer.EnvironmentName == environmentName) layers.Add(layer); }
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
        [YarnCommand("show")]
        public static void Show(string itemName) {
            var characterDisplay = GetCharacterDisplay(itemName);
            characterDisplay?.Show();
            var environmentLayers = GetEnvironmentLayers(itemName);
            foreach (var layer in environmentLayers) { layer?.Show(); }
        }

        [YarnCommand("hide")]
        public static void Hide(string itemName) {
            var characterDisplay = GetCharacterDisplay(itemName);
            characterDisplay?.Hide();
            var environmentLayers = GetEnvironmentLayers(itemName);
            foreach (var layer in environmentLayers) { layer?.Hide(); }
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