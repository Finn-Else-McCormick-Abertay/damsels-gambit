using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public partial class Console : Node
{
    private static Console Instance { get; set; }

    private ConsoleWindow _window;
    private StringBuilder _logBuilder = new();

    public static string LogText => Instance?._logBuilder.ToString();

    public override void _EnterTree() {
        Instance = this;
        var consoleWindowScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/console/console_window.tscn");
        _window = consoleWindowScene.Instantiate<ConsoleWindow>();
        var canvasLayer = new CanvasLayer { Layer = 100 };
        AddChild(canvasLayer); canvasLayer.Owner = this;
        canvasLayer.AddChild(_window); _window.Owner = canvasLayer;
        _window.Hide();
    }
    
    private StringName ToggleActionName = "console_toggle";

    public override void _Input(InputEvent @event) {
        if (_window is null) return;

        if (@event.IsActionPressed(ToggleActionName)) {
            _window.Visible = !_window.Visible;
            if (_window.Visible) { _window.TextEdit.CallDeferred(Control.MethodName.GrabFocus); }
        }
    }

    public static event Action<string> OnPrint;
    public static event Action OnClear;

    public static void Clear() {
        Instance?._logBuilder?.Clear();
        OnClear?.Invoke();
    }

    public static void PrintRaw(string msg) {
        Instance?._logBuilder?.Append(msg);
        OnPrint?.Invoke(msg);
    }

    public static void Print(string msg) {
        PrintRaw($"{msg}\n");
    }

    public static void Info(string msg, bool pushToStdOut = true) {
        Print(msg);
        if (pushToStdOut) { GD.Print(msg); }
    }
    public static void Warning(string msg, bool pushToStdOut = true) {
        PrintRaw($"[color=yellow]Warning: {msg}[/color]\n");
        if (pushToStdOut) { GD.PushWarning(msg); }
    }
    public static void Error(string msg, bool pushToStdOut = true) {
        PrintRaw($"[color=red]Error: {msg}[/color]\n");
        if (pushToStdOut) { GD.PushError(msg); }
    }

    public static void RunCommand(string command) {
        Warning($"No such command '{command}'");
    }

    public static string GetAutofillSuggestion(string command) {
        var args = command.Split();

        var inProgressArg = args.Last();
        if (inProgressArg == "") return "";

        bool Matches(string validArg) {
            if (validArg.Length < inProgressArg.Length) return false;
            return validArg[..inProgressArg.Length] == inProgressArg;
        }

        List<string> validArgs = ["switch", "scene"];
        validArgs.Sort();

        foreach (var validArg in validArgs) { if (Matches(validArg)) return validArg; }

        return "";
    }
}