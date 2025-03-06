using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using CommandLine;
using System.IO;
using CommandLine.Text;
using System.Reflection;
using CsvHelper;
using DamselsGambit.Util;
using System.Collections.ObjectModel;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public sealed partial class Console : Node
{
    private static Console Instance { get; set; }

    private ConsoleWindow _window;
    private readonly Parser _parser;
    private readonly StringBuilder _logBuilder = new();
    
    public static string LogText => Instance?._logBuilder.ToString();

    private readonly Dictionary<string, Command> _commands = [];

    public static ReadOnlyCollection<string> CommandNames => Instance?._commands.Keys.ToList().AsReadOnly();
    public static Command GetCommand(string name) {
        if (Instance is null || Instance?._commands is null) return null;
        Instance._commands.TryGetValue(name, out Command command); return command;
    }

    public static event Action<string> OnPrint;
    public static event Action OnClear;

    public Console() {
        _parser = new(with => { with.AutoHelp = false; with.AutoVersion = false; with.HelpWriter = null; });

        var commandTypes = Assembly.GetAssembly(GetType()).GetTypes().Where(type => typeof(Command).IsAssignableFrom(type) && !type.IsAbstract);
        foreach (var commandType in commandTypes) {
            var commandAttribute = commandType.GetCustomAttribute<CommandAttribute>();
            string name = commandAttribute?.Name ?? Case.ToKebab(commandType.Name);
            var commandInstance = Activator.CreateInstance(commandType) as Command;
            _commands.Add(name, commandInstance);
        }
    }
    
    public override void _EnterTree() {
        if (Instance is not null) throw AutoloadException.For(this);
        Instance = this;
        
        var consoleWindowScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/console/console_window.tscn");
        _window = consoleWindowScene.Instantiate<ConsoleWindow>();
        var canvasLayer = new CanvasLayer { Layer = 100 };
        AddChild(canvasLayer); canvasLayer.Owner = this;
        canvasLayer.AddChild(_window); _window.Owner = canvasLayer;
        _window.Hide();

        Print($"{ProjectSettings.GetSetting("application/config/name").AsString()} v{ProjectSettings.GetSetting("application/config/version").AsString()}");
    }
    
    private static readonly StringName ToggleActionName = "console_toggle";
    public override void _Input(InputEvent @event) {
        if (_window is null) return;

        if (@event.IsActionPressed(ToggleActionName)) {
            _window.Visible = !_window.Visible;
            if (_window.Visible) { _window.TextEdit.CallDeferred(Control.MethodName.GrabFocus); }
            InputManager.ShouldOverrideGuiInput = !_window.Visible;
        }
    }

    public abstract class Command
    {
        public abstract void Parse(Parser parser, IEnumerable<string> args);
        public virtual IEnumerable<string> GetAutofill(string[] args) => [];
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute(string name) : Attribute
    {
        public string Name { get; set; } = name;
    }

    public static void Clear() {
        Instance?._logBuilder?.Clear();
        OnClear?.Invoke();
    }

    public static void PrintRaw(string msg) { Instance?._logBuilder?.Append(msg); OnPrint?.Invoke(msg); }

    public static void Print(string msg) => PrintRaw($"{msg}\n");

    public static void Info(string msg, bool pushToStdOut = true) { Print(msg); if (pushToStdOut) GD.Print(msg); }
    public static void Warning(string msg, bool pushToStdOut = true) { string richMsg = $"[color=#ffde66]{msg}[/color]"; Print(richMsg); if (pushToStdOut) GD.PrintRich(richMsg); }
    public static void Error(string msg, bool pushToStdOut = true) { Print($"[color=red]{msg}[/color]"); if (pushToStdOut) GD.PrintErr(msg); }

    public static void ParseCommand(string inputString) {
        if (Instance is null) return;

        var args = inputString.Split();
        if (args.IsEmpty()) return;
        var commandName = args.FirstOrDefault()?.ToLower();
        if (Instance._commands.TryGetValue(commandName, out Command command)) {
            command.Parse(Instance._parser, args.Length == 1 ? [] : args[1..]);
        }
        else Error($"No such command '{commandName}'.", false);
    }

    public static IEnumerable<string> GetAutofillSuggestions(string inputString) {
        if (inputString == "") return [""];

        var args = inputString.Split();
        var inProgressArg = args.Last();

        bool Matches(string validArg) {
            if (validArg.Length < inProgressArg.Length) return false;
            return validArg[..inProgressArg.Length] == inProgressArg;
        }

        int argIndex = args.Length - 1;
        List<string> validArgs = [];
        if (argIndex == 0) { validArgs.AddRange(Instance._commands.Keys); }
        else {
            if (Instance._commands.TryGetValue(args.First().ToLower(), out Command command)) {
                validArgs.AddRange(command.GetAutofill(args.Length == 1 ? [] : args[1..]));
            }
        }
        validArgs.Sort();

        List<string> suggestions = [];
        foreach (var validArg in validArgs) { if (Matches(validArg)) { suggestions.Add(validArg); } }
        return suggestions;
    }
}