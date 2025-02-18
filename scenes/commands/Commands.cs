using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Godot;

namespace DamselsGambit.Commands;

public class Commands : Console.Command
{
    class Options {}

    public override void Parse(Parser parser, IEnumerable<string> args)
    {
        var result = parser.ParseArguments<Options>(args);
        result.WithParsed(options => {
            foreach (var name in Console.CommandNames) { Console.Info($"{name}"); }
        });

        result.WithNotParsed(err => {
            var helpText = HelpText.AutoBuild(result, h => {
                h.Heading = new HeadingInfo(nameof(Commands));
                h.Copyright = ""; h.AutoHelp = true; h.AutoVersion = false;
                return h;
            }, e => e);
            Console.Info(helpText, false);
        });
    }
}