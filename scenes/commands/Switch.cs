using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Godot;

namespace DamselsGambit.Commands;

public class Switch : Console.Command
{
    public Switch() {
        _scenePaths = [];
        void AddPath(string path) { _scenePaths.Add(path); if (path[..6] == "res://") { _scenePaths.Add(path[6..]); } if (path[..13] == "res://scenes/") { _scenePaths.Add(path[13..]); } }
        void TraverseAndAddScenes(string path) {
            foreach (var fileName in DirAccess.GetFilesAt(path)) { if (Path.GetExtension(fileName) == ".tscn") { AddPath($"{path}/{fileName}"); } }
            foreach (var dirName in DirAccess.GetDirectoriesAt(path)) { TraverseAndAddScenes($"{path}/{dirName}"); }
        }
        TraverseAndAddScenes("res://scenes");
    }
    private readonly List<string> _scenePaths;

    class Options
    {
        [Value(0)]
        public string ScenePath { get; set; }
    }

    public override void Parse(Parser parser, IEnumerable<string> args)
    {
        var result = parser.ParseArguments<Options>(args)
            .WithParsed(options => {
                SwitchToScene(options.ScenePath);
            });

        result.WithNotParsed(err => {
            var helpText = HelpText.AutoBuild(result, h => {
                h.Heading = new HeadingInfo(nameof(Switch));
                h.Copyright = ""; h.AutoHelp = true; h.AutoVersion = false;
                return h;
            }, e => e);
            Console.Info(helpText, false);
        });
    }

    private static void SwitchToScene(string scenePath) {
        string normalisedPath = scenePath;
        if (scenePath.Length >= 6 && scenePath[..6] == "res://") { normalisedPath = normalisedPath[6..]; }
        var lastDot = scenePath.LastIndexOf('.');
        if (lastDot != -1 && scenePath[(lastDot + 1)..] == "tscn") { normalisedPath = normalisedPath[..lastDot]; }
        normalisedPath += ".tscn";

        if (!ResourceLoader.Exists($"res://{normalisedPath}") && ResourceLoader.Exists($"res://scenes/{normalisedPath}")) { normalisedPath = $"scenes/{normalisedPath}"; }

        if (ResourceLoader.Exists($"res://{normalisedPath}")) {
            var scene = ResourceLoader.Load<PackedScene>($"res://{normalisedPath}");
            GameManager.Instance.GetTree().ChangeSceneToPacked(scene);
            
            Console.Info($"Switched to {normalisedPath}");
        }
        else { Console.Error($"Failed to switch to scene {normalisedPath}: no such scene exists."); }
    }
    
    public override IEnumerable<string> GetAutofill(string[] args) {
        if (args.Length <= 1) { return _scenePaths; }
        return [];
    }
}