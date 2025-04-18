using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DamselsGambit.Environment;
using DamselsGambit.Util;
using Godot;
using YarnSpinnerGodot;

namespace DamselsGambit.Dialogue;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public partial class DialogueManager : Node
{
    public static DialogueManager Instance { get; private set; }
    public override void _EnterTree() { if (Instance is not null) throw AutoloadException.For(this); Instance = this; InitRunner(); }

    public static bool VerboseLogging { get; set; } = false;

    public static DialogueRunner Runner { get; private set; }
    public static DialogueRunner ProfileRunner { get; private set; }
    
    public static readonly Knowledge Knowledge = new();

    private static YarnProject _yarnProject;
    private static TextLineProvider _textLineProvider;
    private static VariableStorageBehaviour _variableStorage;
    
    private readonly HashSet<Node> _dialogueViews = [];

    public static ReadOnlyCollection<DialogueView> DialogueViews => Instance?._dialogueViews?.Where(x => x is DialogueView)?.Select(x => x as DialogueView)?.ToList()?.AsReadOnly();

    // Called by dialogue views. Automatically updates our list when views enter or exit tree, and registers view with corresponding runner.
    public static void Register<TView>(TView view) where TView : Node, DialogueViewBase { Instance?._dialogueViews?.Add(view); (view switch { ProfileDialogueView => ProfileRunner, _ => Runner })?.OnReady(x => x.dialogueViews.Add(view)); }
    public static void Deregister<TView>(TView view) where TView : Node, DialogueViewBase { Instance?._dialogueViews?.Remove(view); (view switch { ProfileDialogueView => ProfileRunner, _ => Runner })?.OnReady(x => x.dialogueViews.Remove(view)); }

    public void Reset() {
        Runner?.Stop();
        AnimationDialogueCommands.FlushCommandQueue();
        InitRunner();
        EnvironmentManager.Instance.ReloadEnvironments();
        Knowledge.Reset();
    }

    private void InitRunner(bool force = true) {
        if (Runner is not null) {
            if (force) { RemoveChild(Runner); Runner.QueueFree(); Runner = null; } else return;
        }
        if (ProfileRunner is not null) {
            if (force) { RemoveChild(ProfileRunner); ProfileRunner.QueueFree(); ProfileRunner = null; } else return;
        }

        _yarnProject ??= ResourceLoader.Load<YarnProject>("res://assets/dialogue/DamselsGambit.yarnproject");

        if (_textLineProvider is null) { _textLineProvider = new TextLineProvider(); AddChild(_textLineProvider); }

        if (_variableStorage.IsValid() && force) { _variableStorage.QueueFree(); _variableStorage = null; }
        if (_variableStorage is null) { _variableStorage = new InMemoryVariableStorage(); AddChild(_variableStorage); }

        Runner = new DialogueRunner { Name = "DialogueRunner", yarnProject = _yarnProject, lineProvider = _textLineProvider, variableStorage = _variableStorage, startAutomatically = false, verboseLogging = false };
        ProfileRunner = new DialogueRunner { Name = "ProfileRunner", yarnProject = _yarnProject, lineProvider = _textLineProvider, variableStorage = _variableStorage, startAutomatically = false, verboseLogging = false };

        AddChild(Runner); AddChild(ProfileRunner);

        Runner.Connect(DialogueRunner.SignalName.onDialogueComplete, OnDialogueComplete);

        Runner.SetDialogueViews(_dialogueViews.Where(x => x is not ProfileDialogueView));
        ProfileRunner.SetDialogueViews(_dialogueViews.Where(x => x is ProfileDialogueView));

        Runner.Dialogue.NodeStartHandler += OnNodeStart;
        Runner.Dialogue.NodeCompleteHandler += OnNodeComplete;
        Runner.Dialogue.LineHandler += OnLine;
        Runner.Dialogue.OptionsHandler += OnOptions;
        Runner.Dialogue.CommandHandler += OnCommand;

        foreach (var node in _dialogueViews) (node as DialogueView)?.ResetUsedOptions();
    }

    // Yarn project contains dialogue node with given name
    public static bool DialogueExists(string nodeName) => Runner.Dialogue.NodeExists(nodeName);

    public static IEnumerable<string> GetNodeNames() => Runner.Dialogue.NodeNames;

    // Run given dialogue node. If force is true, will interrupt and overwrite whatever dialogue is already in progress
    // If orErrorDialogue is true, will run the node 'error' (which displays the text 'Invalid node') if it can't find the node
    public static DialogueResult Run(string nodeName, bool force = true, bool orErrorDialogue = true, bool autoPrintErr = true) {
        if (Runner.IsDialogueRunning && !force) return DialogueResult.Err(nodeName, DialogueError.AlreadyRunning, autoPrintErr);
        if (Runner.IsDialogueRunning) { _completesToIgnore++; Runner.Stop(); }

		if (DialogueExists(nodeName)) { Runner.StartDialogue(nodeName); return DialogueResult.Ok(nodeName); }
        else if (orErrorDialogue) Runner.StartDialogue("error");
        return DialogueResult.Err(nodeName, DialogueError.InvalidNode, autoPrintErr);
    }

    // Attempt to run dialogue node, doing nothing if it does not exist (cleaner wrapper for Run with orErrorDialogue set to false)
    public static DialogueResult TryRun(string nodeName, bool force = true) => Run(nodeName, force, false, false);

    public enum DialogueError { None, InvalidNode, AlreadyRunning }

    public class DialogueResult
    {
        public bool Success { get; private set; }
        public DialogueError Error { get; private set; }

        private readonly string _nodeName;

        // Run callback on dialogue end (convenience wrapper for DialogueManager.OnComplete, just to make code neater)
        public void AndThen(Callable callable) => OnComplete(callable);
        public void AndThen(Action action) => OnComplete(Callable.From(action));
        
        public DialogueResult Inspect(Action<DialogueResult> inspect) { if (Success) inspect(this); return this; } // Run callback if successful
        public DialogueResult InspectErr(Action<DialogueResult> inspect) { if (!Success) inspect(this); return this; } // Run callback if failure
        
        internal static DialogueResult Ok(string nodeName) => new(nodeName, true, DialogueError.None);
        internal static DialogueResult Err(string nodeName, DialogueError error, bool autoPrint = true) { var result = new DialogueResult(nodeName, false, error); if (autoPrint) Console.Error(result); return result; }

        private DialogueResult(string nodeName, bool success, DialogueError error) { _nodeName = nodeName; Success = success; Error = error; }

        public static implicit operator bool(DialogueResult result) => result.Success;
        public override string ToString() => Success switch {
            true => $"Successfully ran dialogue node '{_nodeName}'.",
            false => $"Failed to run dialogue node '{_nodeName}': " + Error switch {
                DialogueError.InvalidNode => "No such node.",
                DialogueError.AlreadyRunning => "Dialogue already running and force set to false.",
                _ => ""
            }
        };
    }
    
    private static readonly Stack<string> _dialogueStack = [];
    public static void Push(string nodeName) {
        if (!DialogueExists(nodeName)) Console.Warning($"Pushed nonexistent node '{nodeName}' to dialogue stack.");
        _dialogueStack.Push(nodeName);
    }
    
    public static void Pop(bool ignoreEmpty = false) {
        if (_dialogueStack.TryPop(out string node)) TryRun(node).InspectErr(err => Console.Error($"Error while popping from dialogue stack: {err}"));
        else if (!ignoreEmpty) Console.Warning("Attempted to pop from dialogue stack while empty.");
    }

    private static int _completesToIgnore = 0;
    private static readonly Queue<Callable> _onCompleteQueue = [];

    private void OnDialogueComplete() {
        if (_completesToIgnore > 0) { _completesToIgnore--; return; }

        while (_onCompleteQueue.Count > 0) {
            var callable = _onCompleteQueue.Dequeue();
            if (callable.Target is null || callable.Target.IsValid()) callable.CallDeferred();
            else Console.Error("Dialogue OnComplete callback failed: target is invalid.");
        }
    }

    private static string LineToDebugString(Yarn.Line line) {
        var localisedLine = _textLineProvider.GetLocalizedLine(line);
        return $"{localisedLine.RawText}{(localisedLine.Metadata is not null ? $" {string.Join(" ", localisedLine.Metadata.Select(x => $"#{x}"))}" : "")}";
    }

    private void OnNodeStart(string nodeName) {
        if (!VerboseLogging) return;
        var tags = Runner.Dialogue.GetTagsForNode(nodeName) ?? [];
        Console.Info($"Dialogue: --- {nodeName}{(tags.Any() ? $"({string.Join(", ", tags)})" : "")} ---");
    }

    private void OnNodeComplete(string nodeName) {
        if (!VerboseLogging) return;
        Console.Info($"Dialogue: ===");
    }

    private void OnLine(Yarn.Line line) {
        if (!VerboseLogging) return;
        Console.Info($"Dialogue: {LineToDebugString(line)}");
    }

    private void OnOptions(Yarn.OptionSet options) {
        if (!VerboseLogging) return;
        var currentNode = Runner.yarnProject.Program.Nodes[Runner.Dialogue.CurrentNode];
        //Console.Info($"!{string.Join(", ", currentNode.Instructions.Select(x => $"[{Enum.GetName(x.Opcode)}:{string.Join(", ", x.Operands.Select(op => op.ValueCase switch { Yarn.Operand.ValueOneofCase.StringValue => op.StringValue, Yarn.Operand.ValueOneofCase.FloatValue => op.FloatValue.ToString(), Yarn.Operand.ValueOneofCase.BoolValue => op.BoolValue.ToString(), _ => "null" }))}]"))}");
        foreach (var option in options.Options) {
            var optionInstruction = currentNode.Instructions.Where(x => x.Opcode == Yarn.Instruction.Types.OpCode.AddOption && x.Operands.FirstOrDefault()?.StringValue == option.Line.ID).FirstOrDefault();
            bool isDependent = optionInstruction?.Operands?.LastOrDefault()?.BoolValue ?? false;
            Console.Info($"Dialogue: -> {LineToDebugString(option.Line)}{(!option.IsAvailable ? " (unavailable)" : "")}{(isDependent ? " (dependent)" : "")}{(!option.DestinationNode.IsNullOrWhitespace() ? $" => {option.DestinationNode}" : "")}");
        }
    }

    private void OnCommand(Yarn.Command command) {
        if (!VerboseLogging) return;
        Console.Info($"Dialogue: <<{command.Text}>>");
    }

    public static void OnComplete(Callable callable) {
        if (!Runner.IsDialogueRunning) callable.Call();
        else _onCompleteQueue.Enqueue(callable);
    }
    public static void OnComplete(Action action) => OnComplete(Callable.From(action));
}