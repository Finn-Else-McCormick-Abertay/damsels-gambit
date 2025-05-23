using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DamselsGambit.Util;
using Godot;
using YarnSpinnerGodot;

namespace DamselsGambit.Dialogue;

public partial class ProfileDialogueView : Node, DialogueViewBase
{
	public string ProfileNode { get; set { field = value; Update(); } }

	[Export] private Label Title { get; set; }
	[Export] private RichTextLabel Subtitle { get; set; }
	[Export] private RichTextLabel Label { get; set; }
	[Export] private TextureRect Portrait { get; set; }

	[Export] private Godot.Collections.Dictionary<string, NodePath> Marginalia { get; set; }

	private StringBuilder _stringBuilder;

	public override void _EnterTree() {
		if (Title.IsValid()) Title.Text = ""; if (Subtitle.IsValid()) Subtitle.Text = ""; if (Label.IsValid()) Label.Text = ""; if (Portrait.IsValid()) Portrait.Texture = null;
		DialogueManager.Register(this);
		DialogueManager.Knowledge.OnChanged += Update;
		this.OnReady(Update);
	}
	public override void _ExitTree() {
		DialogueManager.Deregister(this);
		DialogueManager.Knowledge.OnChanged -= Update;
	}

	private void Update() {
		if (!DialogueManager.ProfileRunner.IsValid() || string.IsNullOrEmpty(ProfileNode)) return;
		if (DialogueManager.ProfileRunner.IsDialogueRunning) DialogueManager.ProfileRunner.Stop();
		if (DialogueManager.DialogueExists(ProfileNode)) DialogueManager.ProfileRunner.StartDialogue(ProfileNode); else Console.Warning($"Profile view could not update: '{ProfileNode}' is not a valid dialogue node.");
	}

	public Action requestInterrupt { get; set; }

	public void DialogueStarted() {
		_stringBuilder = new();
		var nodeTags = DialogueManager.ProfileRunner.GetTagsForNode(ProfileNode).Select(x => x.StripFront('#'));
		var complexTags = nodeTags.Select(x => x.Split(':')).Where(x => x.Length == 2).Select(x => (x.First().ToLower(), x.Last())).ToDictionary();

		if (Portrait.IsValid() && complexTags.TryGetValue("portrait", out var path)) Portrait.Texture = ResourceLoader.Load<Texture2D>($"res://{path.Replace('\\','/').StripFront("res://")}");

		foreach (var (name, nodePath) in Marginalia) { var item = GetNodeOrNull<CanvasItem>(nodePath); if (item.IsValid()) item.Hide(); }

		Title.Text = ""; Subtitle.Text = ""; Label.Text = "";
	}

	public void RunLine(LocalizedLine line, Action onLineFinished) {
		if (!Title.IsValid() || !Subtitle.IsValid() || !Label.IsValid() || !Portrait.IsValid()) return;

		bool HasTag(string tag) => line.Metadata?.Any(x => x.MatchN(tag)) ?? false;

		var lineText = line.Text.AsBBCode(out var metaData);

		foreach (var marginaliaName in metaData) { if (Marginalia.TryGetValue(marginaliaName, out var nodePath) && GetNodeOrNull<CanvasItem>(nodePath) is CanvasItem item && item.IsValid()) item.Show(); }

		if (HasTag("title")) Title.Text = lineText;
		else if (HasTag("subtitle")) Subtitle.Text = lineText;
		else if (!string.IsNullOrWhiteSpace(lineText)) _stringBuilder.AppendLine(lineText);

		onLineFinished?.Invoke();
	}

	public void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) => Console.Error("Profile view encountered dialogue options.");
	
	public void DialogueComplete() { if (Label.IsValid()) Label.Text = _stringBuilder?.ToString(); }

}
