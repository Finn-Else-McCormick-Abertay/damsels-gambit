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

    public static event Action<string> OnPrint;
    public static event Action OnClear;
    
    /// <summary> Clear in-game console. </summary>
    public static void Clear() { Instance?._logBuilder?.Clear(); OnClear?.Invoke(); }

    /// <summary> Print <paramref name="msg"/> to in-game console without a final newline. </summary>
    public static void PrintRaw(string msg) { Instance?._logBuilder?.Append(msg); OnPrint?.Invoke(msg); }
    /// <summary> Print <paramref name="msg"/> to in-game console with final newline. </summary>
    public static void Print(string msg) => PrintRaw($"{msg}\n");

    /// <summary> <para>Print basic info to in-game console and the standard output.</para> </summary>
    public static void Info(string msg, bool pushToStdOut = true) { Print(msg); if (pushToStdOut) GD.Print(msg); }
    /// <summary> <para>Print warning message to in-game console and the standard output.</para> </summary>
    public static void Warning(string msg, bool pushToStdOut = true) { string richMsg = $"[color=#ffde66]{msg}[/color]"; Print(richMsg); if (pushToStdOut) GD.PrintRich(richMsg); }
    /// <summary> <para>Print error message to in-game console and the standard error output.</para> </summary>
    public static void Error(string msg, bool pushToStdOut = true) { Print($"[color=red]{msg}[/color]"); if (pushToStdOut) GD.PrintErr(msg); }
    
    /// <summary> <inheritdoc cref="Info(string, bool)"/> <para>Converts objects to string via <see cref="ToStringExtensions.ToPrettyString{T}(T)"/></para> </summary> 
    public static void Info(params object[] args) => Info(string.Join("", args.Select(ToStringExtensions.ToPrettyString)));
    /// <summary> <inheritdoc cref="Warning(string, bool)"/> <para>Converts objects to string via <see cref="ToStringExtensions.ToPrettyString{T}(T)"/></para> </summary> 
    public static void Warning(params object[] args) => Warning(string.Join("", args.Select(ToStringExtensions.ToPrettyString)));
    /// <summary> <inheritdoc cref="Error(string, bool)"/> <para>Converts objects to string via <see cref="ToStringExtensions.ToPrettyString{T}(T)"/></para> </summary> 
    public static void Error(params object[] args) => Error(string.Join("", args.Select(ToStringExtensions.ToPrettyString)));
    
    // These have to exist as separate functions because the way you call C# methods from GdScript doesn't work with default arguments or params

    /// <inheritdoc cref="Info(string, bool)"/>
    public static void Info(string msg) => Info(msg, true);
    /// <inheritdoc cref="Warning(string, bool)"/>
    public static void Warning(string msg) => Warning(msg, true);
    /// <inheritdoc cref="Error(string, bool)"/>
    public static void Error(string msg) => Error(msg, true);
    
    private readonly Dictionary<string, Command> _commands = [];
    public static ReadOnlyCollection<string> CommandNames => Instance?._commands.Keys.ToList().AsReadOnly();

    public static Command GetCommand(string name) {
        if (Instance is null || Instance?._commands is null) return null;
        Instance._commands.TryGetValue(name, out Command command); return command;
    }

    private void RegisterAllCommands() {
        _commands.Clear();

        // Find all types in our assembly which inherit from Command and are constructible
        var commandTypes = Assembly.GetAssembly(GetType()).GetTypes().Where(type => typeof(Command).IsAssignableFrom(type) && !type.IsAbstract);
        foreach (var commandType in commandTypes) {
            var commandAttribute = commandType.GetCustomAttribute<CommandAttribute>();
            string name = commandAttribute?.Name ?? Case.ToKebab(commandType.Name);
            // Create instance of command type
            var commandInstance = Activator.CreateInstance(commandType) as Command;
            _commands.Add(name, commandInstance);
        }
    }
    
    // Any class inheriting this interface will be registered as a command
    public abstract class Command
    {
        public abstract void Parse(Parser parser, IEnumerable<string> args);
        public virtual IEnumerable<string> GetAutofill(string[] args) => [];
    }

    // Can be applied to a command to override the name it is registered under (normally it is the class name converted to kebab case)
    [AttributeUsage(AttributeTargets.Class)] public class CommandAttribute(string name) : Attribute { public string Name { get; set; } = name; }

    // Parse input string and perform command if any
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

    // Get potential autofill to continue given input string. Autofill options start from the beginning of the most recent word.
    public static IEnumerable<string> GetAutofillSuggestions(string inputString) {
        if (inputString == "") return [""];
        var args = inputString.Split(); var inProgressArg = args.LastOrDefault();
        return
            (args.Length switch { <= 1 => (IEnumerable<string>)[..Instance._commands.Keys], > 1 => Instance._commands.TryGetValue(args.First(), out Command command) ? [..command.GetAutofill(args[1..])] : [] })
            .Where(arg => arg.Length >= inProgressArg.Length && arg[..inProgressArg.Length] == inProgressArg)
            .Order();
    }
    
    public Console() {
        _parser = new(with => { with.AutoHelp = false; with.AutoVersion = false; with.HelpWriter = null; });
        RegisterAllCommands();
        ProcessMode = ProcessModeEnum.Always;
    }
    
    public override void _EnterTree() {
        if (Instance is not null) throw AutoloadException.For(this);
        Instance = this;
        
        _window = ResourceLoader.Load<PackedScene>("res://scenes/console/ui/console_window.tscn").Instantiate<ConsoleWindow>();
        var canvasLayer = new CanvasLayer { Layer = 100 };
        AddChild(canvasLayer); canvasLayer.AddChild(_window);
        _window.Hide();

        Print($"{ProjectSettings.GetSetting("application/config/name").AsString()} v{ProjectSettings.GetSetting("application/config/version").AsString()}");
    }
    
    private static readonly StringName ToggleActionName = "console_toggle";
    public override void _Input(InputEvent @event) {
        if (_window is null) return;

        if (@event.IsActionPressed(ToggleActionName)) {
            _window.Visible = !_window.Visible;
            InputManager.ShouldOverrideGuiInput = !_window.Visible;
            if (_window.Visible) { _window.TextEdit.CallDeferred(Control.MethodName.GrabFocus); }
        }
    }
}