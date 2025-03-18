using System.Collections.Generic;
using YarnSpinnerGodot;
using Godot;

namespace DamselsGambit.Dialogue;

static class FlowDialogueCommands
{
    private static readonly Stack<string> _dialogueStack = [];

    [YarnCommand("push")]
    public static void Push(string node) {
        if (node == "__this") node = DialogueManager.Runner?.CurrentNodeName;
        if (!DialogueManager.DialogueExists(node)) Console.Warning($"Pushed nonexistent node '{node}' to dialogue stack.");
        _dialogueStack.Push(node);
    }

    [YarnCommand("pop")]
    public static void Pop() {
        if (_dialogueStack.TryPop(out string node)) DialogueManager.TryRun(node).InspectErr(err => Console.Error($"Error while popping from dialogue stack: {err}"));
        else Console.Warning("Attempted to pop from dialogue stack while empty.");
    }
}