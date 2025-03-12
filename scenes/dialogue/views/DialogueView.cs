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
	[Export] public Control OptionVisualRoot { get; set; }
	[Export] public Control OptionRoot { get; set; }
	[Export] public Control OptionArchetype { get; set { field = value; OptionArchetype?.Hide(); OptionArchetype?.GetParent()?.OnReady(x => x.RemoveChild(OptionArchetype)); } }

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
		OptionVisualRoot.Hide();
		DialogueManager.Register(this);
	}
	public override void _ExitTree() {
		ContinueButton?.TryDisconnect(BaseButton.SignalName.Pressed, OnContinue);
		DialogueManager.Deregister(this);
	}

    public override void _Notification(int what) { if (what == NotificationPredelete) OptionArchetype?.QueueFree(); }

    public void DialogueStarted() {
		Root.Show();
		State = DialogueState.Waiting;
	}

	public void RunLine(LocalizedLine line, Action onLineFinished) {
		_onLineFinishedAction = onLineFinished;

		bool HasTag(string tag) => line?.Metadata?.Contains(tag) ?? false;

		bool withNext = HasTag("withnext") || HasTag("lastline");

		TitleLabel?.Set(Label.PropertyName.Text, line.CharacterName);
		LineLabel?.Set(Label.PropertyName.Text, line.TextWithoutCharacterName.AsBBCode());

		TitleRoot.Visible = line.CharacterName is not null && line.CharacterName != "";
		LineRoot.Visible = true;
		ContinueButton.Visible = !withNext;
		ContinueButton.GrabFocus();

		if (line?.Metadata?.Where(x => x.StartsWith("theme="))?.Select(x => x.StripFront("theme="))?.SingleOrDefault() is string themeName) {
			if (_themes.TryGetValue(themeName, out var theme)) Root.Theme = theme;
			else Console.Warning($"Failed to switch to dialogue theme '{themeName}': theme does not exist.");
		}
		else Root.Theme = _themes.GetValueOrDefault(line?.CharacterName switch {
			"Princess Penelope" => "princess",
			_ => ""
		});
		
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

		Control toFocus = null;

		foreach (var option in dialogueOptions) {
			var optionControl = OptionArchetype.Duplicate() as Control;
			OptionRoot.AddChild(optionControl);

			var button = optionControl as Button ?? optionControl.FindChildOfType<Button>();
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

		OptionVisualRoot.Show();
		State = DialogueState.DisplayingOptions;
	}

	private void CleanupOptions() {
		foreach (var child in OptionRoot.GetChildren()) { if (child != OptionArchetype) { child.QueueFree(); } }
		OptionVisualRoot.Hide();
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
