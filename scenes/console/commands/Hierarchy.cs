using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit.Commands;

public class Hierarchy : Console.Command
{
    class Options {
        [Value(0, MetaName = "Root Node")]
        public string RootPath { get; set; } = "root";

        [Option]
        public bool Internal { get; set; } = false;

        [Option]
        public bool Index { get; set; } = false;
        
        [Option]
        public bool Group { get; set; } = false;
    }

    public override void Parse(Parser parser, IEnumerable<string> args)
    {
        var result = parser.ParseArguments<Options>(args);
        result.WithParsed(options => {
            var rootPath = options.RootPath.StripFront("root").Replace('\\', '/').StripFront('/');

            Node root = string.IsNullOrWhiteSpace(rootPath) ? GameManager.Instance.GetTree().Root : GameManager.Instance.GetTree().Root.GetNode(rootPath);
            if (root is null) { Console.Error($"Invalid node path 'root/{rootPath}'", false); return; }
            
            var children = options.Internal ? root.GetInternalChildren() : root.GetChildren();

            var msg = new StringBuilder();
            msg.AppendLine(NodeInfo(root, options));
            msg.AppendJoin("\n", children.Select(node => $"({node.GetIndex(options.Internal)}) - {NodeInfo(node, options)}"));

            Console.Print("");
            Console.Info(msg.ToString());
        });

        result.WithNotParsed(err => {
            var helpText = HelpText.AutoBuild(result, h => {
                h.Heading = new HeadingInfo(nameof(Clear));
                h.Copyright = ""; h.AutoHelp = true; h.AutoVersion = false;
                return h;
            }, e => e);
            Console.Print(helpText);
        });
    }

    private static string NodeInfo(Node node, Options options) {
        var sb = new StringBuilder();
        sb.Append(node.Name).Append('[');

        var script = node.GetScript().As<Script>();
        if (script is not null) {
            var globalName = script.GetGlobalName();
            if (!string.IsNullOrEmpty(globalName)) sb.Append(globalName); else sb.Append(script.ResourcePath.StripFront("res://"));
            sb.Append(": ").Append(script.GetInstanceBaseType());
        }
        else sb.Append(node.GetClass());
        sb.Append(']');

        List<string> info = [];

        if (options.Index) info.Add($"Index{(options.Internal ? "(Including Internal)" : "")}: {node.GetIndex(options.Internal)}");
        if (node.UniqueNameInOwner) info.Add("Unique in owner");

        if (!string.IsNullOrEmpty(node.SceneFilePath)) info.Add($"File: {node.SceneFilePath.StripFront("res://")}");
        
        var groups = node.GetGroups();
        if (options.Group && groups.Count > 0) info.Add(new StringBuilder().Append("Group").Append(groups.Count > 1 ? "s" : "").Append(": ").Append(groups.Count > 1 ? "[" : "").AppendJoin(", ", groups).Append(groups.Count > 1 ? "]" : "").ToString());

        if (node.GetChildCount() > 0) info.Add($"{node.GetChildCount()} child{(node.GetChildCount() > 1 ? "ren" : "")}");

        if (info.Count > 0) sb.Append('(').AppendJoin(", ", info).Append(')');

        return sb.ToString();
    }
}