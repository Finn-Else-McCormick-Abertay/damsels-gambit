using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Godot;

namespace DamselsGambit.Commands;

#if DEBUG
public class Debug : Console.Command
{
    [Verb("input")]
    class InputOptions {
        [Option]
        public bool Enable { get; set; } = true;
    }

    private static readonly CanvasLayer _debugCanvasLayer = new() { Layer = 50 };
    private static Control _guideDebugger = null;

    public override void Parse(Parser parser, IEnumerable<string> args)
    {
        var result = parser.ParseArguments<InputOptions>(args);
        result.WithParsed<InputOptions>(options => {
            if (options.Enable) {
                if (!_debugCanvasLayer.IsInsideTree()) {
                    GameManager.Instance.AddChild(_debugCanvasLayer);
                }
                if (_guideDebugger is null) {
                    _guideDebugger = ResourceLoader.Load<PackedScene>("res://addons/guide/debugger/guide_debugger.tscn").Instantiate<Control>();
                    _debugCanvasLayer.AddChild(_guideDebugger);
                }
                _guideDebugger.Show();
            }
            else {
                _guideDebugger?.Hide();
            }
        });

        result.WithNotParsed(err => {
            var helpText = HelpText.AutoBuild(result, h => {
                h.Heading = new HeadingInfo(nameof(Debug));
                h.Copyright = ""; h.AutoHelp = true; h.AutoVersion = false;
                return h;
            }, e => e);
            Console.Info(helpText, false);
        });
    }

    
    public override IEnumerable<string> GetAutofill(string[] args) {
        if (args.Length == 1) return [ "input" ];
        return [];
    }
}
#endif