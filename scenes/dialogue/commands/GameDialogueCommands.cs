using System.Collections.Generic;
using YarnSpinnerGodot;
using Godot;

namespace DamselsGambit.Dialogue;

static class GameDialogueCommands
{
    [YarnCommand("score")]
    public static void Score(int val) => GameManager.CardGameController.Score += val;

    [YarnCommand("learn")]
    public static void Learn(string id) => DialogueManager.Knowledge.Learn(id);

    [YarnCommand("unlearn")]
    public static void Unlearn(string id) => DialogueManager.Knowledge.Unlearn(id);
    
    [YarnFunction("knows")]
    public static bool Knows(string id) => DialogueManager.Knowledge.Knows(id);
}