using System;
using System.Text;
using DamselsGambit.Util;
using Godot;
using YarnSpinnerGodot;

namespace DamselsGambit.Dialogue;

public partial class ProfileDialogueView : Node, DialogueViewBase
{
    public Action requestInterrupt { get; set; }
    
    public string ProfileNode { get; set; }

    [Export] private RichTextLabel Label { get; set; }

    private StringBuilder _stringBuilder;

    public override void _EnterTree() {
        DialogueManager.Register(this);
        DialogueManager.Knowledge.OnChanged += Update;
        this.OnReady(Update);
    }
    public override void _ExitTree() {
        DialogueManager.Deregister(this);
        DialogueManager.Knowledge.OnChanged -= Update;
    }

    private void Update() {
        if (DialogueManager.ProfileRunner.IsDialogueRunning) DialogueManager.ProfileRunner.Stop();
        if (DialogueManager.DialogueExists(ProfileNode)) DialogueManager.ProfileRunner.StartDialogue(ProfileNode); else Console.Warning($"Profile view could not update: '{ProfileNode}' is not a valid dialogue node.");
    }

    public static void SetupDialogueCommands(DialogueRunner runner) {
        //runner.AddCommandHandler("", () => {});
    }

    public void DialogueStarted() {
        _stringBuilder = new();
    }

    public void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        _stringBuilder.AppendLine(dialogueLine.Text.Text);
        onDialogueLineFinished?.Invoke();
	}

	public void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        onDialogueLineFinished?.Invoke();
	}

	public void DismissLine(Action onDismissalComplete) {
        onDismissalComplete?.Invoke();
	}

	public void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) => Console.Error("Profile view encountered dialogue options.");
    
	public void DialogueComplete() {
        Label.Text = _stringBuilder.ToString();
    }

	public void UserRequestedViewAdvancement() {}
}