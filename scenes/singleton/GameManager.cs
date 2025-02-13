using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public static CardGameController CardGameController { get; private set; }

    public override void _EnterTree() { Instance = this; }
    public override void _Ready() {
        if (Console.Initialise(GetTree().Root.GetNode("LimboConsole"))) { RegisterCommands(); }
        //if (GUIDE.Initialise(GetTree().Root.GetNode("GUIDE"))) { GUIDE.EnableMappingContext(GUIDE.MappingContextDefault); }
        CardGameController = GetTree().Root.FindChildOfType<CardGameController>();
    }

    public void QuitToTitle() {
        DialogueManager.Instance.InitRunner();
        var mainNode = GetTree().Root.GetNode("Main");
        GetTree().Root.RemoveChild(mainNode); mainNode.QueueFree();

        var mainScene = ResourceLoader.Load<PackedScene>("res://scenes/main.tscn");
        var newMainNode = mainScene.Instantiate();
        GetTree().Root.AddChild(newMainNode);
        GetTree().Paused = false;

        CardGameController = GetTree().Root.FindChildOfType<CardGameController>();
    }

    private void RegisterCommands() {
        List<string> scenePaths = [];
        void AddPath(string path) { scenePaths.Add(path); if (path[0..7] == "scenes/") { scenePaths.Add(path[7..]); } }
        void TraverseAndAddScenes(string path) {
            foreach (var fileName in DirAccess.GetFilesAt(path)) { if (Path.GetExtension(fileName) == ".tscn") { AddPath($"{path}/{fileName}"); } }
            foreach (var dirName in DirAccess.GetDirectoriesAt(path)) { TraverseAndAddScenes($"{path}/{dirName}"); }
        }
        TraverseAndAddScenes("res://scenes");

        Console.RegisterCommand<string>("switch", "switch scenes", SwitchScenes, new Dictionary<int, IEnumerable<string>> { { 1, scenePaths } });

        Console.RegisterCommand("run", "", (string node) => {
            DialogueManager.Run(node, true, false);         
        }, new Dictionary<int, Func<Godot.Collections.Array>> { { 1, () => new Godot.Collections.Array(DialogueManager.Runner.yarnProject.Program.Nodes.Select(x => Variant.From(x.Key))) } });

        Console.RegisterCommand<string>("get", "", GetCommand, new Dictionary<int, IEnumerable<string>> { { 1, [ "cards" ] } });
    }

    private void SwitchScenes(string scenePath) {
        string normalisedPath = scenePath;
        if (scenePath.Substr(0, 7) == "res://") { normalisedPath = normalisedPath[7..]; }
        var lastDot = scenePath.LastIndexOf('.');
        if (lastDot != -1 && scenePath[(lastDot + 1)..] == "tscn") { normalisedPath = normalisedPath[..lastDot]; }
        normalisedPath += ".tscn";

        if (!ResourceLoader.Exists($"res://{normalisedPath}") && ResourceLoader.Exists($"res://scenes/{normalisedPath}")) { normalisedPath = $"scenes/{normalisedPath}"; }

        if (ResourceLoader.Exists($"res://{normalisedPath}")) {
            var scene = ResourceLoader.Load<PackedScene>($"res://{normalisedPath}");
            GetTree().ChangeSceneToPacked(scene);
            
            Console.Info($"Switched to {normalisedPath}");
        }
        else { Console.Error($"Failed to switch to scene {normalisedPath}: no such scene exists."); }
    }

    private void GetCommand(string what) {
        if (what == "cards") {
            var sb = new StringBuilder();
            sb.Append("Subject Deck  : [").AppendJoin(", ", CardGameController.RemainingSubjectDeck).Append("]\n");
            sb.Append("Modifier Deck : [").AppendJoin(", ", CardGameController.RemainingModifierDeck).Append(']');

            Console.Info(sb.ToString());
        }
    }
} 