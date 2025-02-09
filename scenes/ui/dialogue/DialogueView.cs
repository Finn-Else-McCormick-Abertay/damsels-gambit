using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using YarnSpinnerGodot;
using DamselsGambit.Util;

namespace DamselsGambit.Dialogue;

public partial class DialogueView : Node, DialogueViewBase
{
	[Export] public Control TitleRoot { get; set { field = value; TitleLabel = TitleRoot is null ? null : TitleRoot is RichTextLabel ? TitleRoot as RichTextLabel : (Control)TitleRoot?.FindChildOfType<RichTextLabel>() ?? TitleRoot?.FindChildOfType<Label>(); } }
	[Export] public Control LineRoot { get; set { field = value; LineLabel = LineRoot is null ? null : LineRoot is RichTextLabel ? LineRoot as RichTextLabel : (Control)LineRoot?.FindChildOfType<RichTextLabel>() ?? TitleRoot?.FindChildOfType<Label>(); } }
	[Export] public Control OptionRoot { get; set; }
	[Export] public Control OptionArchetype { get; set { field = value; OptionArchetype?.Hide(); } }

	public Control TitleLabel { get; private set; }
	public Control LineLabel { get; private set; }

	[Export] Button ContinueButton { get; set { ContinueButton?.TryDisconnect(Button.SignalName.Pressed, OnContinue); field = value; ContinueButton?.TryConnect(Button.SignalName.Pressed, OnContinue); } }

	public enum DialogueState { Inactive, DisplayingLine, DisplayingOptions, Waiting }
	public DialogueState State { get; private set; }

	Action _onLineFinishedAction = null;

    public Action requestInterrupt { get; set; }

	public override void _EnterTree() {
		ContinueButton?.TryConnect(Button.SignalName.Pressed, OnContinue);
	}
	public override void _ExitTree() {
		ContinueButton?.TryDisconnect(Button.SignalName.Pressed, OnContinue);
		TitleRoot = TitleRoot;
		LineRoot = LineRoot;
	}

	public void DialogueStarted() {
		State = DialogueState.Waiting;
		TitleRoot.Hide();
		LineRoot.Hide();
		ContinueButton.Hide();
		OptionRoot.Hide();
	}

    public void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        static void TrySetText(Control control, string text) {
			if (control is Label label) { label.Text = text; }
			if (control is RichTextLabel richTextLabel) { richTextLabel.Text = text; }
		}
		TrySetText(TitleLabel, dialogueLine.CharacterName);
		TrySetText(LineLabel, dialogueLine.TextWithoutCharacterName.Text);
		TitleRoot.Show();
		LineRoot.Show();
		ContinueButton.Show();
		_onLineFinishedAction = onDialogueLineFinished;
		State = DialogueState.DisplayingLine;
	}

	private void OnContinue() {
		if (State != DialogueState.DisplayingLine) return;

		_onLineFinishedAction?.Invoke();
	}

	public void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
        onDialogueLineFinished?.Invoke();
    }

	public void DismissLine(Action onDismissalComplete) {
		ContinueButton.Hide();
        onDismissalComplete?.Invoke();
		State = DialogueState.Waiting;
    }

	public void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) {
		CleanupOptions();
		foreach (var child in OptionRoot.GetChildren()) { if (child != OptionArchetype) { child.QueueFree(); } }
		OptionArchetype.Hide();

		foreach (var option in dialogueOptions) {
			var optionControl = OptionArchetype.Duplicate() as Control;
			optionControl.GetParent()?.RemoveChild(optionControl); OptionRoot.AddChild(optionControl);

			var button = optionControl.FindChildOfType<Button>();
			button.Text = option.Line.Text.Text;
			button.Pressed += () => {
				Task.Factory.StartNew(async () => {
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					CleanupOptions();
					onOptionSelected?.Invoke(option.DialogueOptionID);
				});
			};

			optionControl.Show();
		}

		OptionRoot.Show();
		State = DialogueState.DisplayingOptions;
    }

	private void CleanupOptions() {
		foreach (var child in OptionRoot.GetChildren()) { if (child != OptionArchetype) { child.QueueFree(); } }
		OptionArchetype.Hide();
		OptionRoot.Hide();
		State = DialogueState.Waiting;
	}

	public void DialogueComplete() {
		State = DialogueState.Inactive;
		TitleRoot.Hide();
		LineRoot.Hide();
	}

	public void UserRequestedViewAdvancement() {}
}
