using System.Collections.Generic;
using YarnSpinnerGodot;
using Godot;

namespace DamselsGambit.Dialogue;

static class FlowDialogueCommands
{
    [YarnCommand("push")]
    public static void Push(string node) {
        DialogueManager.Push(node switch { "this" => DialogueManager.Runner?.CurrentNodeName, _ => node });
    }

    [YarnCommand("pop")]
    public static void Pop() {
        DialogueManager.Pop();
    }
}