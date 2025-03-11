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

    private StringBuilder _stringBuilder;

    public override void _EnterTree() {
        Title.Text = ""; Subtitle.Text = ""; Label.Text = ""; Portrait.Texture = null;
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
    }

    public void RunLine(LocalizedLine line, Action onLineFinished) {
        bool HasTag(string tag) => line.Metadata?.Any(x => x.MatchN(tag)) ?? false;

        if (HasTag("title")) Title.Text = line.Text.AsBBCode();
        else if (HasTag("subtitle")) Subtitle.Text = line.Text.AsBBCode();
        else _stringBuilder.AppendLine(line.Text.AsBBCode());

        onLineFinished?.Invoke();
	}

	public void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) => Console.Error("Profile view encountered dialogue options.");
    
	public void DialogueComplete() { Label.Text = _stringBuilder?.ToString(); }

}