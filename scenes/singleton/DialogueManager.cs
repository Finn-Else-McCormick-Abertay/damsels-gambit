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
        Runner = new DialogueRunner {
            yarnProject = ResourceLoader.Load<YarnProject>("res://assets/dialogue/DamselsGambit.yarnproject"),
            startAutomatically = false
        };
        AddChild(Runner);
        //Runner.SetDialogueViews(_dialogueViews);
        Runner.Ready += () => { Runner.SetDialogueViews(_dialogueViews); };
    }

    private readonly HashSet<CharacterDisplay> _characterDisplays = [];
    private readonly HashSet<DialogueView> _dialogueViews = [];

    public static void RegisterDisplay(CharacterDisplay display) {
        if (Instance is null) return;
        Instance._characterDisplays.Add(display);
    }
    public static void DeregisterDisplay(CharacterDisplay display) {
        if (Instance is null) return;
        Instance._characterDisplays.Remove(display);
    }

    public static void RegisterView(DialogueView view) {
        if (Instance is null) return;
        Instance._dialogueViews.Add(view);
        if (Runner is not null && Runner.IsNodeReady()) {
            Runner.dialogueViews.Add(view);
        }
    }
    public static void DeregisterView(DialogueView view) {
        if (Runner is null) return;
        Instance._dialogueViews.Remove(view);
        if (Runner is not null && Runner.IsNodeReady()) {
            Runner.dialogueViews.Remove(view);
        }
    }

    public static CharacterDisplay GetCharacterDisplay(string characterName) {
        if (Instance is null) return null;
        foreach (var node in Instance._characterDisplays) {
            if (node is CharacterDisplay display && display.CharacterName == characterName) return display;
        }
        return null;
    }

    public static bool Run(string nodeName, bool force = false, bool orErrorDialogue = true) {
        if (Runner.IsDialogueRunning) { if (force) { Runner.Stop(); } else { return false; } }
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
        [YarnCommand("emote")]
        public static void Emote(string characterName, string emotionName) {
            var display = GetCharacterDisplay(characterName);
            if (display is null) return;
            display.SpriteName = emotionName;
        }

        [YarnCommand("score")]
        public static void Score(int val) {
            GameManager.CardGameController.Score += val;
        }
    }
}