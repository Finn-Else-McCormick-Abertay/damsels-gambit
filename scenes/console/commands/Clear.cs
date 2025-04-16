using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Godot;

namespace DamselsGambit.Commands;

public class Clear : Console.Command
{
    class Options {}

    public override void Parse(Parser parser, IEnumerable<string> args)
    {
        var result = parser.ParseArguments<Options>(args);
        result.WithParsed(options => { Console.Clear(); });

        result.WithNotParsed(err => {
            var helpText = HelpText.AutoBuild(result, h => {
                h.Heading = new HeadingInfo(nameof(Clear));
                h.Copyright = ""; h.AutoHelp = true; h.AutoVersion = false;
                return h;
            }, e => e);
            Console.Print(helpText);
        });
    }
}