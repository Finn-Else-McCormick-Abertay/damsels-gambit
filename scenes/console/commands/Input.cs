using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bridge;
using CommandLine;
using CommandLine.Text;
using Godot;

namespace DamselsGambit.Commands;

public class Input : Console.Command
{
    [Verb("context")]
    class ContextOptions {
        [Value(0)]
        public IEnumerable<string> Value { get; set; }
        [Option(SetName = "get")]
        public bool Get { get; set; }

        //[Option]
        //public bool? All { get; set; } = null;

        //[Option]
        //public bool Enabled { get; set; }
    }

    [Verb("action")]
    class ActionOptions {
        //[Option(SetName = "get")]
        //public IEnumerable<string> Get { get; set; } = null;
    }

    [Verb("", true)]
    class DefaultOptions {
        [Value(0)]
        public IEnumerable<string> Value { get; set; }
        [Option(SetName = "get")]
        public bool Get { get; set; }
    }

    public override void Parse(Parser parser, IEnumerable<string> args)
    {
        var result = parser.ParseArguments<ContextOptions, ActionOptions, DefaultOptions>(args);
        result.WithParsed<ContextOptions>(options => {
            if (options.Get) {
                OutputMappingContexts();
            }
            else { OutputHelp(result); }
        });
        result.WithParsed<ActionOptions>(options => {
        });

        result.WithParsed<DefaultOptions>(options => {
            if (options.Get) {
                if (options.Value.Any(x => x.MatchN("focus"))) {
                    var focusOwner = InputManager.Instance.GetViewport().GuiGetFocusOwner();
                    Console.Info($"{focusOwner}");
                }
            }
        });

        result.WithNotParsed(err => OutputHelp(result));
    }

    private void OutputHelp(ParserResult<object> result) {
        var helpText = HelpText.AutoBuild(result, h => {
            h.Heading = new HeadingInfo(nameof(Input));
            h.Copyright = ""; h.AutoHelp = true; h.AutoVersion = false;
            return h;
        }, e => e);
        Console.Info(helpText, false);
    }
    
    public override IEnumerable<string> GetAutofill(string[] args) {
        if (args.Length == 1) return [ "context", "action" ];
        var verb = args.First();

        if (verb == "context") {
            return [ "--get" ];
        }

        if (verb == "action") {
        }

        return [];
    }

    private static void OutputMappingContexts() {
        var sb = new StringBuilder();
        foreach (var context in GUIDE.GetEnabledMappingContexts()) {
            sb.AppendLine($"{context.DisplayName}");
            sb.AppendLine(string.Join(", ", context.Mappings.Select(x => GUIDEActionMapping.From(x.As<Resource>())).Select(x => GUIDEAction.From(x.Action.As<Resource>()))
                .Select(action => $"{action.Name} - {action.ActionValueType.AsInt32() switch { 0 => "BOOL", 1 => "AXIS_1D", 2 => "AXIS_2D", 3 => "AXIS_3D", _ => "???" }}")));
        }
        Console.Info(sb.ToString());
    }
}