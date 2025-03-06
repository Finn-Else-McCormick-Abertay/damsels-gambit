using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using YarnSpinnerGodot;
using DamselsGambit.Util;
using System.Collections.Generic;
using System.IO;

namespace DamselsGambit.Dialogue;

public partial class DialogueView : Node, DialogueViewBase
{
	[Export] public Control Root { get; set; }
	[Export] public Control TitleRoot { get; set { field = value; TitleLabel = TitleRoot is null ? null : TitleRoot is RichTextLabel ? TitleRoot as RichTextLabel : (Control)TitleRoot?.FindChildOfType<RichTextLabel>() ?? TitleRoot?.FindChildOfType<Label>(); } }
	[Export] public Control LineRoot { get; set { field = value; LineLabel = LineRoot is null ? null : LineRoot is RichTextLabel ? LineRoot as RichTextLabel : (Control)LineRoot?.FindChildOfType<RichTextLabel>() ?? TitleRoot?.FindChildOfType<Label>(); } }
	[Export] public Control OptionRoot { get; set; }
	[Export] public Control OptionArchetype { get; set { field = value; OptionArchetype?.Hide(); } }

	public Control TitleLabel { get; private set; }
	public Control LineLabel { get; private set; }

	[Export] Button ContinueButton { get; set { ContinueButton?.TryDisconnect(BaseButton.SignalName.Pressed, OnContinue); field = value; ContinueButton?.TryConnect(BaseButton.SignalName.Pressed, OnContinue); } }

	private static readonly Dictionary<string, Theme> _themes = [];
	static DialogueView() {
		foreach (var (fullPath, relativePath) in FileUtils.GetFilesOfTypeAbsoluteAndRelative<Theme>("res://assets/ui/theme/dialogue/"))
			_themes.Add(relativePath.StripExtension(), ResourceLoader.Load<Theme>(fullPath));
	}

	public enum DialogueState { Inactive, DisplayingLine, DisplayingOptions, Waiting }
	public DialogueState State { get; private set; }

	Action _onLineFinishedAction = null;

	public Action requestInterrupt { get; set; }

	public override void _EnterTree() {
		ContinueButton?.TryConnect(BaseButton.SignalName.Pressed, OnContinue);
		Root.Hide();
		TitleRoot.Hide();
		LineRoot.Hide();
		ContinueButton.Hide();
		OptionRoot.Hide();
		DialogueManager.Register(this);
	}
	public override void _ExitTree() {
		ContinueButton?.TryDisconnect(BaseButton.SignalName.Pressed, OnContinue);
		DialogueManager.Deregister(this);
	}

	public override void _Ready() {
		OptionArchetype?.GetParent()?.RemoveChild(OptionArchetype);
	}
    public override void _Notification(int what) {
		if (what == NotificationPredelete) OptionArchetype?.QueueFree();
    }

    public void DialogueStarted() {
		Root.Show();
		State = DialogueState.Waiting;
	}

	public void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
		_onLineFinishedAction = onDialogueLineFinished;

		bool withNext = dialogueLine?.Metadata?.Contains("withnext") ?? false;

		TitleLabel?.Set(Label.PropertyName.Text, dialogueLine.CharacterName);
		LineLabel?.Set(Label.PropertyName.Text, dialogueLine.TextWithoutCharacterName.Text);

		TitleRoot.Visible = dialogueLine.CharacterName is not null && dialogueLine.CharacterName != "";
		LineRoot.Visible = true;
		ContinueButton.Visible = !withNext;
		ContinueButton.GrabFocus();

		var themeTag = (dialogueLine?.Metadata?.Where(x => x.StartsWith("theme=")) ?? []).SingleOrDefault();
		if (themeTag is not null) {
			var themeName = themeTag.StripFront("theme=");
			if (_themes.TryGetValue(themeName, out var theme)) {
				Root.Theme = theme;
			}
			else Console.Warning($"Failed to switch to dialogue theme '{themeName}': theme does not exist.");
		}
		else Root.Theme = null;
		
		State = DialogueState.DisplayingLine;
		if (withNext) { _onLineFinishedAction?.Invoke(); }
	}

	private void OnContinue() {
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

		Control toFocus = null;

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
			toFocus ??= button;

			optionControl.Show();
		}

		toFocus?.GrabFocus();

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
		Root.Hide();
	}

	public void UserRequestedViewAdvancement() {}
}
