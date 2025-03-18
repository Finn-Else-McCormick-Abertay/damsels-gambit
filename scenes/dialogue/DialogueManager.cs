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

    public static DialogueRunner Runner { get; private set; }
    public static DialogueRunner ProfileRunner { get; private set; }
    
    public static readonly Knowledge Knowledge = new();

    private static YarnProject _yarnProject;
    private static TextLineProvider _textLineProvider;
    private static VariableStorageBehaviour _variableStorage;
    
    private readonly HashSet<Node> _dialogueViews = [];

    public static ReadOnlyCollection<DialogueView> DialogueViews => Instance?._dialogueViews?.Where(x => x is DialogueView)?.Select(x => x as DialogueView)?.ToList()?.AsReadOnly();

    public static void Register<TView>(TView view) where TView : Node, DialogueViewBase { Instance?._dialogueViews?.Add(view); (view switch { ProfileDialogueView => ProfileRunner, _ => Runner })?.OnReady(x => x.dialogueViews.Add(view)); }
    public static void Deregister<TView>(TView view) where TView : Node, DialogueViewBase { Instance?._dialogueViews?.Remove(view); (view switch { ProfileDialogueView => ProfileRunner, _ => Runner })?.OnReady(x => x.dialogueViews.Remove(view)); }

    public override void _EnterTree() {
        if (Instance is not null) throw AutoloadException.For(this);
        Instance = this; InitRunner();
    }

    public void Reset() {
        Runner?.Stop();
        AnimationDialogueCommands.FlushCommandQueue();
        InitRunner();
        EnvironmentManager.Instance.ReloadEnvironments();
        Knowledge.Reset();
    }

    private void InitRunner(bool force = true) {
        if (Runner is not null) { if (force) { RemoveChild(Runner); Runner.QueueFree(); Runner = null; } else return; }
        if (ProfileRunner is not null) { if (force) { RemoveChild(ProfileRunner); ProfileRunner.QueueFree(); ProfileRunner = null; } else return; }

        _yarnProject ??= ResourceLoader.Load<YarnProject>("res://assets/dialogue/DamselsGambit.yarnproject");

        if (_textLineProvider is null) { _textLineProvider = new TextLineProvider(); this.AddOwnedChild(_textLineProvider); }

        if (_variableStorage.IsValid() && force) { _variableStorage.QueueFree(); _variableStorage = null; }
        if (_variableStorage is null) { _variableStorage = new InMemoryVariableStorage(); this.AddOwnedChild(_variableStorage); }

        Runner = new DialogueRunner { Name = "DialogueRunner", yarnProject = _yarnProject, lineProvider = _textLineProvider, variableStorage = _variableStorage, startAutomatically = false };
        ProfileRunner = new DialogueRunner { Name = "ProfileRunner", yarnProject = _yarnProject, lineProvider = _textLineProvider, variableStorage = _variableStorage, startAutomatically = false };

        this.AddOwnedChild(Runner); this.AddOwnedChild(ProfileRunner);

        Runner.Connect(DialogueRunner.SignalName.onDialogueStart, new Callable(this, MethodName.OnRunnerDialogueStart));
        Runner.Connect(DialogueRunner.SignalName.onDialogueComplete, new Callable(this, MethodName.OnRunnerDialogueComplete));
        Runner.Connect(DialogueRunner.SignalName.onNodeStart, new Callable(this, MethodName.OnRunnerNodeStart));
        Runner.Connect(DialogueRunner.SignalName.onNodeComplete, new Callable(this, MethodName.OnRunnerNodeComplete));

        Runner.SetDialogueViews(_dialogueViews.Where(x => x is not ProfileDialogueView));
        ProfileRunner.SetDialogueViews(_dialogueViews.Where(x => x is ProfileDialogueView));
    }

    public static bool DialogueExists(string nodeName) => _yarnProject?.Program?.Nodes?.ContainsKey(nodeName ?? "") ?? false;

    public static DialogueResult Run(string nodeName, bool force = true, bool orErrorDialogue = true) {
        if (force) Runner.Stop(); else if (Runner.IsDialogueRunning) return new DialogueResult(nodeName, false, "Dialogue already running and force set to false.");
		if (DialogueExists(nodeName)) {
			Runner.StartDialogue(nodeName);
            return new DialogueResult(nodeName, true);
		}
        var errorMsg = $"No such node '{nodeName}'";
        Console.Warning(errorMsg);
        if (orErrorDialogue) {
            Runner.StartDialogue("error");
            return new DialogueResult("error", false, errorMsg);
        }
        return new DialogueResult(nodeName, false, errorMsg);
    }

    public static DialogueResult TryRun(string nodeName, bool force = true) => Run(nodeName, force, false);

    public class DialogueResult
    {
        private readonly string _node;

        public bool Success { get; private set; }
        public string Error { get; private set; }

        public static implicit operator bool(DialogueResult result) => result.Success;
        
        internal DialogueResult(string node, bool success, string error = "") { _node = node; Success = success; Error = error; }

        public void AndThen(Callable callable) => OnComplete(callable);
        public void AndThen(Action action) => OnComplete(Callable.From(action));

        public void InspectErr(Action<string> inspect) { if (!Success) inspect(Error); }
    }

    private void OnRunnerDialogueStart() {
        Console.Info("OnStart");
    }

    private void OnRunnerDialogueComplete() {
        Console.Info("OnComplete");
    }

    private void OnRunnerNodeStart(string nodeName) {
        Console.Info($"OnNodeStart {nodeName}");
    }

    private void OnRunnerNodeComplete(string nodeName) {
        Console.Info($"OnNodeComplete {nodeName}");
    }

    public static void OnComplete(Callable callable) {
        if (!Runner.IsDialogueRunning) callable.Call();
        else CallableUtils.CallDeferred(() => Runner.Connect(DialogueRunner.SignalName.onDialogueComplete, callable, (uint)ConnectFlags.OneShot));
    }
    public static void OnComplete(Action action) => OnComplete(Callable.From(action));
}