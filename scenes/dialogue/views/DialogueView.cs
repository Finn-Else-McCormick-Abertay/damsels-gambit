using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using YarnSpinnerGodot;
using DamselsGambit.Util;
using System.Collections.Generic;
using System.IO;
using Yarn;

namespace DamselsGambit.Dialogue;

public partial class DialogueView : Control, DialogueViewBase, IFocusContext, IFocusableContainer
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

	public static class ThemeTypeVariations {
		public static readonly StringName OptionNormal = "DialogueOptionButton";
		public static readonly StringName OptionUsed = "DialogueOptionButtonUsed";
		public static readonly StringName OptionNewlyUnlocked = "DialogueOptionButtonUnlocked";
	}

	public enum DialogueState { Inactive, DisplayingLine, DisplayingOptions, Waiting }
	public DialogueState State { get; private set; }

	Action _onLineFinishedAction = null;

	public Action requestInterrupt { get; set; }

	private readonly HashSet<string> _usedOptions = [];
	public void ResetUsedOptions() => _usedOptions.Clear();

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
		ContinueButton.Hide();
		foreach (var child in OptionRoot.GetChildren().Where(x => x != OptionArchetype)) child.QueueFree();
		State = DialogueState.Waiting;
	}

	public void RunLine(LocalizedLine line, Action onLineFinished) {
		_onLineFinishedAction = onLineFinished;

		bool HasTag(string tag) => line?.Metadata?.Contains(tag) ?? false;
		bool withNext = HasTag("withnext") || HasTag("lastline");

		var lineText = line.TextWithoutCharacterName.AsBBCode();
		if (lineText.IsNullOrWhitespace()) lineText = "";

		TitleLabel?.Set(Label.PropertyName.Text, line.CharacterName);
		LineLabel?.Set(Label.PropertyName.Text, lineText);

		if (LineLabel is RichTextLabel richLabel && richLabel.GetLineCount() == 1 && !withNext) richLabel.Text += "\n ";

		TitleRoot.Visible = line.CharacterName is not null && line.CharacterName != "";
		LineRoot.Visible = true;//!string.IsNullOrWhiteSpace(line.TextWithoutCharacterName.Text);
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
		if (withNext) _onLineFinishedAction?.Invoke();
	}

	public void HideBox() {
		TitleRoot.Hide();
		LineRoot.Hide();
		OptionVisualRoot.Hide();
		ContinueButton.Hide();
	}

	private void OnContinue() => _onLineFinishedAction?.Invoke();

	public void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
		ContinueButton.Hide();
		onDialogueLineFinished?.Invoke();
		State = DialogueState.Waiting;
	}

	public void DismissLine(Action onDismissalComplete) {
		_onLineFinishedAction = null;
		ContinueButton.Hide();
		onDismissalComplete?.Invoke();
		State = DialogueState.Waiting;
	}

	public void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) {
		_onLineFinishedAction = null;
		CleanupOptions();

        var currentNode = DialogueManager.Runner.yarnProject.Program.Nodes[DialogueManager.Runner.Dialogue.CurrentNode];

		Control toFocus = null;

		foreach (var option in dialogueOptions) {
			if (!option.IsAvailable) continue;

			var tags = option.Line.Metadata ?? [];

			var optionControl = OptionArchetype.Duplicate() as Control;
			OptionRoot.AddChild(optionControl);
			
			bool alreadyUsed = _usedOptions.Contains(option.Line.TextID);
            var optionInstruction = currentNode.Instructions.Where(x => x.Opcode == Yarn.Instruction.Types.OpCode.AddOption && x.Operands.FirstOrDefault()?.StringValue == option.Line.TextID).FirstOrDefault();
            bool isDependent = optionInstruction?.Operands?.LastOrDefault()?.BoolValue ?? false;

			string styleString = null;
			foreach (var rawTag in tags.Where(x => x.StartsWith("style="))) {
				string tag = rawTag;
				bool shouldUse = true;
				if (rawTag.Contains(';')) {
					var tagSplit = rawTag.Split(';', 2); tag = tagSplit[0];
					string validation = tagSplit[1];
					if (validation.Contains('=')) {
						var validationSplit = validation.Split('=', 2);
						if (validationSplit[0] == "knows" && !validationSplit[1].Split(',').Any(DialogueManager.Knowledge.Knows)) shouldUse = false;
					}
				}
				if (shouldUse) styleString = tag.StripFront("style=");
			}

			Console.Info($"{option.Line.Text.Text} : {tags.ToPrettyString()} : Style = {styleString.ToPrettyString()}");
			
			var button = optionControl as Button ?? optionControl.FindChildOfType<Button>();
			button.Text = option.Line.Text.Text;
			button.ThemeTypeVariation = 0 switch {
				_ when styleString is not null => styleString,
				_ when alreadyUsed => ThemeTypeVariations.OptionUsed,
				_ when isDependent => ThemeTypeVariations.OptionNewlyUnlocked,
				_ => ThemeTypeVariations.OptionNormal
			};
			button.Pressed += () => {
				Task.Factory.StartNew(async () => {
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					_usedOptions.Add(option.Line.TextID);
					CleanupOptions(); onOptionSelected?.Invoke(option.DialogueOptionID);
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
		foreach (var child in OptionRoot.GetChildren()) child.QueueFree();
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
	
    public virtual int FocusContextPriority => State != DialogueState.Inactive ? 3 : -1;

    public Control GetDefaultFocus() => ContinueButton;
}
