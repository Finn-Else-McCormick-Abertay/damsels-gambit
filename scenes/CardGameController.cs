using DamselsGambit;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using YarnSpinnerGodot;

public partial class CardGameController : Control
{
	[Export] public string[] SubjectDeck { get; set; } = [];
	[Export] public string[] ModifierDeck { get; set; } = [];

	private List<string> _subjectDeckWorking, _modifierDeckWorking;

	[Export] AffectionMeter AffectionMeter { get; set; }
	[Export] HandContainer SubjectHand { get; set; }
	[Export] HandContainer ModifierHand { get; set; }
	[Export] Button PlayButton { get; set; }

	private readonly int _scoreMax = 10, _scoreMin = -10;
	public int Score { get; set { field = value; AffectionMeter.ValuePercent = 1 - ((Score / (float)(Math.Abs(_scoreMax) + Math.Abs(_scoreMin))) + Math.Abs(_scoreMin) / (float)(Math.Abs(_scoreMax) + Math.Abs(_scoreMin))); } }

	public override void _Ready() {
		PlayButton?.TryConnect(Button.SignalName.Pressed, new Callable(this, MethodName.PlayHand));
		var suitor = DialogueManager.GetCharacterDisplay("suitor");
		if (suitor is not null) { suitor.SpriteName = "neutral"; }

		foreach (var child in SubjectHand.GetChildren()) { SubjectHand.RemoveChild(child); child.QueueFree(); }
		foreach (var child in ModifierHand.GetChildren()) { ModifierHand.RemoveChild(child); child.QueueFree(); }

		_subjectDeckWorking = SubjectDeck.Select(x => x).ToList();
		_modifierDeckWorking = ModifierDeck.Select(x => x).ToList();

		//TODO: Shuffle

		Deal();
	}

	public override void _Process(double delta) {
		PlayButton.Disabled = SubjectHand.GetSelected().Count() != 1 || ModifierHand.GetSelected().Count() != 1;
	}

	private void PlayHand() {
		var selectedSubject = SubjectHand.GetSelected().First();
		var selectedModifier = ModifierHand.GetSelected().First();

		var cardId = $"{selectedModifier.CardId}+{selectedSubject.CardId}";

		selectedSubject.QueueFree();
		selectedModifier.QueueFree();
		PlayButton.Hide();
		
		DialogueManager.Runner.TryConnect(DialogueRunner.SignalName.onDialogueComplete, new Callable(this, MethodName.OnDialogueComplete));

		DialogueManager.Run(cardId);
	}

	private void OnDialogueComplete() {
		DialogueManager.Runner.TryDisconnect(DialogueRunner.SignalName.onDialogueComplete, new Callable(this, MethodName.OnDialogueComplete));
		PlayButton.Show();
		Deal();
	}

	private void Deal() {
        static void InternalDeal(HandContainer container, List<string> workingDeck) {
			int cardsToDeal = Math.Max(3 - container.GetChildCount(), 0);
			for (int i = 0; i < cardsToDeal; ++i) {
				var cardId = workingDeck.First();
				workingDeck.RemoveAt(0);
				var cardDisplay = new CardDisplay{ CardId = cardId, ShadowOffset = new(-10, 1), ShadowOpacity = 0.4f };
				container.AddChild(cardDisplay);
				cardDisplay.Owner = container;
			}
		}
		InternalDeal(SubjectHand, _subjectDeckWorking);
		InternalDeal(ModifierHand, _modifierDeckWorking);
	}
}
