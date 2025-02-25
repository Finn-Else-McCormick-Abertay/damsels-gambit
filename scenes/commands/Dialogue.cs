using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using DamselsGambit.Dialogue;
using Godot;

namespace DamselsGambit.Commands;

public class Dialogue : Console.Command
{
    [Verb("get")]
    class GetOptions {
        [Option]
        public string Scene { get; set; }
        [Option]
        public string Character { get; set; }

        [Option]
        public bool Scenes { get; set; }
        [Option]
        public bool Characters { get; set; }
    }

    [Verb("run")]
    class RunOptions {
        [Value(0, HelpText = "Dialogue node to trigger", MetaName = "Node")]
        public string Node { get; set; }
    }

    public override void Parse(Parser parser, IEnumerable<string> args)
    {
        var result = parser.ParseArguments<GetOptions, RunOptions>(args);
        result.WithParsed<GetOptions>(options => {
            if (!string.IsNullOrEmpty(options.Scene)) {
                var items = DialogueManager.GetEnvironmentItems(options.Scene);
                if (!items.Any()) Console.Error($"No such environment '{options.Scene}'", false);
                else Console.Info(string.Join(", ", items.Select(x => $"{x.Name} ({(x.Get(CanvasItem.PropertyName.Visible).AsBool() ? "Visible" : "Hidden")})")), false);
            }
            if (!string.IsNullOrEmpty(options.Character)) {
                var display = DialogueManager.GetCharacterDisplay(options.Character);
                if (display is null) Console.Error($"No such character '{options.Character}'", false);
                else Console.Info($"{display.Name}: {display.SpriteName}", false);
            }
            if (options.Scenes) Console.Info(string.Join(", ", DialogueManager.GetEnvironmentNames()), false);
            if (options.Characters) Console.Info(string.Join(", ", DialogueManager.GetCharacterNames()), false);
        });
        result.WithParsed<RunOptions>(options => {
            DialogueManager.Run(options.Node, true);
        });

        result.WithNotParsed(err => {
            var helpText = HelpText.AutoBuild(result, h => {
                h.Heading = new HeadingInfo(nameof(Dialogue));
                h.Copyright = ""; h.AutoHelp = true; h.AutoVersion = false;
                return h;
            }, e => e);
            Console.Info(helpText, false);
        });
    }
}