using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using DamselsGambit.Dialogue;
using DamselsGambit.Environment;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit.Commands;

public class Dialogue : Console.Command
{
    [Verb("get")]
    class GetOptions {
        [Option]
        public string Node { get; set; }
        [Option]
        public string Scene { get; set; }
        [Option]
        public string Character { get; set; }
        [Option]
        public string Prop { get; set; }

        [Option]
        public bool Nodes { get; set; }
        [Option]
        public bool Scenes { get; set; }
        [Option]
        public bool Characters { get; set; }
        [Option]
        public bool Props { get; set; }
        [Option]
        public string Knowledge { get; set; }
        [Option]
        public bool Facts { get; set; }
    }

    [Verb("run")]
    class RunOptions {
        [Value(0, HelpText = "Dialogue node to trigger", MetaName = "Node")]
        public string Node { get; set; }
    }

    [Verb("learn")]
    class LearnOptions {
        [Value(0, HelpText = "Fact to learn", MetaName = "Fact")]
        public string Fact { get; set; }
    }

    [Verb("unlearn")]
    class UnlearnOptions {
        [Value(0, HelpText = "Fact to unlearn", MetaName = "Fact")]
        public string Fact { get; set; }
    }

    public override void Parse(Parser parser, IEnumerable<string> args)
    {
        var program = DialogueManager.Runner.yarnProject.Program;
        var result = parser.ParseArguments<GetOptions, RunOptions, LearnOptions, UnlearnOptions>(args);
        result.WithParsed<GetOptions>(options => {
            if (!options.Node.IsEmpty()) {
                if (program.Nodes.TryGetValue(options.Node, out var node)) {
                    var sb = new StringBuilder();
                    sb.Append(node.Name);
                    if (node.Tags.Count > 0) sb.Append(" - Tags: [").AppendJoin(", ", node.Tags).Append(']');
                    if (node.Labels.Count > 0) sb.Append(" - Labels: ").AppendJoin(", ", node.Labels.Select(x => $"{x.Key}:{x.Value}"));
                    sb.AppendLine().AppendLine("---");

                    List<string> dialogueOptions = [];
                    bool showingDialogue = false;

                    foreach (var instruction in node.Instructions) {
                        if (program.InitialValues.TryGetValue(node.SourceTextStringID, out var operand)) {
                            Console.Info(operand.StringValue);
                            continue;
                        }
                        switch (instruction.Opcode) {
                            case Yarn.Instruction.Types.OpCode.Jump: case Yarn.Instruction.Types.OpCode.Pop: case Yarn.Instruction.Types.OpCode.Stop: break;
                            case Yarn.Instruction.Types.OpCode.RunCommand: {
                                sb.AppendLine($"[color=LightBlue]<<{string.Join(' ', instruction.Operands.First().StringValue)}>>[/color]");
                            } break;
                            case Yarn.Instruction.Types.OpCode.RunLine: {
                                sb.Append(DialogueManager.Runner.yarnProject.baseLocalization.GetLocalizedString(instruction.Operands.First().StringValue));
                                sb.Append(' ').AppendJoin(' ', (DialogueManager.Runner.yarnProject.LineMetadata.GetMetadata(instruction.Operands.First().StringValue) ?? []).Select(x => $"[color=Salmon]#{x}[/color]"));
                                sb.AppendLine();
                            } break;
                            case Yarn.Instruction.Types.OpCode.AddOption: {
                                dialogueOptions.Add(DialogueManager.Runner.yarnProject.baseLocalization.GetLocalizedString(instruction.Operands.First().StringValue));
                            } break;
                            case Yarn.Instruction.Types.OpCode.ShowOptions: {
                                var nextDialogueOption= dialogueOptions.FirstOrDefault();
                                if (nextDialogueOption is not null) {
                                    dialogueOptions = dialogueOptions[1..];
                                    sb.Append("-> ").AppendLine(nextDialogueOption);
                                    sb.Append("[indent]");
                                }
                                showingDialogue = true;                          
                            } break;
                            default: {
                                bool isGroupEnd = instruction.Operands.FirstOrDefault().StringValue.EndsWith("group_end");
                                if (showingDialogue && isGroupEnd) {
                                    sb.Append("[/indent]");
                                    var nextDialogueOption= dialogueOptions.FirstOrDefault();
                                    if (nextDialogueOption is not null) {
                                        dialogueOptions = dialogueOptions[1..];
                                        sb.Append("-> ").AppendLine(nextDialogueOption);
                                        sb.Append("[indent]");
                                    }
                                    if (dialogueOptions.Count == 0) { showingDialogue = false; }
                                }
                                else if (isGroupEnd) sb.Append("[/indent]");
                                else sb.AppendLine(instruction.ToString());
                            } break;
                        }
                    }

                    sb.AppendLine("===");
                    
                    Console.Info(sb.ToString());
                }
                else Console.Error($"No such node '{options.Node}'.");
            }
            if (!options.Scene.IsEmpty()) {
                var items = EnvironmentManager.GetEnvironmentItems(options.Scene);
                if (!items.Any()) Console.Error($"No such environment '{options.Scene}'", false);
                else Console.Info(string.Join(", ", items.Select(x => $"{x.Name} ({(x.Get(CanvasItem.PropertyName.Visible).AsBool() ? "Visible" : "Hidden")})")));
            }
            if (!options.Character.IsEmpty()) {
                var displays = EnvironmentManager.GetCharacterDisplays(options.Character);
                if (displays.Count == 0) Console.Error($"No such character '{options.Character}'", false);
                else Console.Info(string.Join(", ", displays.Select(x => $"{x.Name}: {x.SpriteName}")));
            }
            if (!options.Prop.IsEmpty()) {
                var displays = EnvironmentManager.GetPropDisplays(options.Prop);
                if (displays.Count == 0) Console.Error($"No such prop '{options.Prop}'", false);
                else Console.Info(string.Join(", ", displays.Select(x => $"{x.PropName}: {x.Variant}")));
            }

            if (options.Knowledge == "all" || options.Facts) Console.Info(string.Join(", ", Knowledge.AllFacts));
            else if (options.Knowledge == "current") Console.Info(string.Join(", ", DialogueManager.Knowledge.KnownFacts));
            else Console.Error($"Knowledge: unknown arg '{options.Knowledge}'");

            
            if (options.Nodes) Console.Info(string.Join(", ", program.Nodes.Keys));
            if (options.Scenes) Console.Info(string.Join(", ", EnvironmentManager.GetEnvironmentNames()));
            if (options.Characters) Console.Info(string.Join(", ", EnvironmentManager.GetCharacterNames()));
            if (options.Props) Console.Info(string.Join(", ", EnvironmentManager.GetPropNames()));
        });

        result.WithParsed<RunOptions>(options => {
            DialogueManager.Run(options.Node, true);
        });

        result.WithParsed<LearnOptions>(options => { DialogueManager.Knowledge.Learn(options.Fact); });

        result.WithParsed<UnlearnOptions>(options => { DialogueManager.Knowledge.Unlearn(options.Fact); });

        result.WithNotParsed(err => {
            var helpText = HelpText.AutoBuild(result, h => {
                h.Heading = new HeadingInfo(nameof(Dialogue));
                h.Copyright = program.Name; h.AutoHelp = true; h.AutoVersion = false;
                return h;
            }, e => e);
            Console.Print(helpText);
        });
    }

    public override IEnumerable<string> GetAutofill(string[] args) {
        if (args.Length == 1) return [ "run", "learn", "unlearn", "get" ];
        if (args.Length > 1) return args.First() switch {
            "get" => args.Length switch {
                2 => [ "--node", "--nodes", "--scene", "--scenes", "--character", "--characters", "--prop", "--props", "--knowledge", "--facts" ],
                > 2 when args.Contains("--knowledge") => [ "current", "all" ],
                > 2 when args.Contains("--node") => DialogueManager.Runner.yarnProject.Program.Nodes.Keys,
                > 2 when args.Contains("--scene") => EnvironmentManager.GetEnvironmentNames(),
                > 2 when args.Contains("--character") => EnvironmentManager.GetCharacterNames(),
                > 2 when args.Contains("--prop") => EnvironmentManager.GetPropNames(),
                _ => []
            },
            "run" when args.Length == 2 => DialogueManager.Runner.yarnProject.Program.Nodes.Keys,
            "learn" or "unlearn" when args.Length == 2 => Knowledge.AllFacts,
            _ => []
        };
        return [];
    }
}