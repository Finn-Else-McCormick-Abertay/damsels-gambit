using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit.Commands;

public class Switch : Console.Command
{
    public Switch() {
        _scenePaths = [];
        foreach (var file in FileUtils.GetFilesOfType<PackedScene>()) { _scenePaths.Add(file); if (file.StartsWith("res://")) _scenePaths.Add(file.StripFront("res://")); }
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
            Console.Print(helpText);
        });
    }

    private static void SwitchToScene(string scenePath) {
        scenePath = scenePath.StripFront("res://").ReplaceExtension(".tscn");

        if (!ResourceLoader.Exists($"res://{scenePath}") && ResourceLoader.Exists($"res://scenes/{scenePath}")) scenePath = $"scenes/{scenePath}";

        if (ResourceLoader.Exists($"res://{scenePath}")) {
            var scene = ResourceLoader.Load<PackedScene>($"res://{scenePath}");
            GameManager.Instance.GetTree().ChangeSceneToPacked(scene);
            
            Console.Info($"Switched to {scenePath}");
        }
        else { Console.Error($"Failed to switch to scene {scenePath}: no such scene exists."); }
    }
    
    public override IEnumerable<string> GetAutofill(string[] args) {
        if (args.Length <= 1) { return _scenePaths; }
        return [];
    }
}