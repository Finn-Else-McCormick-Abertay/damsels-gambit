using System.Collections.Generic;
using Godot;
using YarnSpinnerGodot;

namespace DamselsGambit.Dialogue;

public partial class DialogueManager : Node
{
    public static DialogueManager Instance { get; private set; }

    public override void _EnterTree() { Instance = this; }

    private readonly HashSet<NodePath> _characterDisplayPaths = [];

    public static void RegisterDisplay(CharacterDisplay display) {
        Instance._characterDisplayPaths.Add(display.GetPath());
        GD.Print($"Register Display for {display.CharacterName}");
    }
    public static void DeregisterDisplay(CharacterDisplay display) {
        Instance._characterDisplayPaths.Remove(display.GetPath());
        GD.Print($"Deregister Display for {display.CharacterName}");
    }

    public static CharacterDisplay GetCharacterDisplay(string characterName) {
        if (Instance is null) return null;
        foreach (var path in Instance._characterDisplayPaths) {
            var node = Instance.GetTree().Root.GetNode(path);
            if (node is CharacterDisplay display && display.CharacterName == characterName) return display;
        }
        return null;
    }

    // Dialogue commands
    static class Commands
    {    
        [YarnCommand("emote")]
        public static void Emote(string characterName, string emotionName) {
            GD.Print($"Emote {emotionName} on {characterName}");
            var display = GetCharacterDisplay(characterName);
            if (display is null) return;
            display.SpriteName = emotionName;
        }

    }
}