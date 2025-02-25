using DamselsGambit;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using YarnSpinnerGodot;

public partial class CardGameController : Control
{
	public readonly int MaxRound = 8;
	private int Round { get; set { field = value; if (RoundMeter is not null) { RoundMeter.CurrentRound = Round; } } }

	[Export] public string[] SubjectDeck { get; set; } = [];
	[Export] public string[] ModifierDeck { get; set; } = [];

	private List<string> _subjectDeckWorking, _modifierDeckWorking;

	public ReadOnlyCollection<string> RemainingSubjectDeck => _subjectDeckWorking.AsReadOnly();
	public ReadOnlyCollection<string> RemainingModifierDeck => _modifierDeckWorking.AsReadOnly();

	[Export] AffectionMeter AffectionMeter { get; set; }
	[Export] HandContainer SubjectHand { get; set; }
	[Export] HandContainer ModifierHand { get; set; }
	[Export] Button PlayButton { get; set; }
	[Export] RoundMeter RoundMeter { get; set; }

	private readonly int _scoreMax = 10, _scoreMin = -10;
	private readonly int _loveThreshold = 4, _hateThreshold = -4;
	public int Score { get; set {
			field = value;
			var newDisplayValue = 1 - ((Score / (float)(Math.Abs(_scoreMax) + Math.Abs(_scoreMin))) + Math.Abs(_scoreMin) / (float)(Math.Abs(_scoreMax) + Math.Abs(_scoreMin)));
			var valueTween = AffectionMeter.CreateTween();
			valueTween.TweenProperty(AffectionMeter, AffectionMeter.PropertyName.ValuePercent.ToString(), newDisplayValue, 1.0);
		}
	}

	public override void _Ready() {
		AffectionMeter.LovePercent = (Math.Abs(_scoreMax) - Math.Abs(_loveThreshold)) / (float)(Math.Abs(_scoreMax) + Math.Abs(_scoreMin));
		AffectionMeter.HatePercent = (Math.Abs(_scoreMin) - Math.Abs(_hateThreshold)) / (float)(Math.Abs(_scoreMax) + Math.Abs(_scoreMin));

		PlayButton?.TryConnect(Button.SignalName.Pressed, new Callable(this, MethodName.PlayHand));
		var suitor = DialogueManager.GetCharacterDisplay("suitor");
		if (suitor is not null) { suitor.SpriteName = "neutral"; }

		foreach (var child in SubjectHand.GetChildren()) { SubjectHand.RemoveChild(child); child.QueueFree(); }
		foreach (var child in ModifierHand.GetChildren()) { ModifierHand.RemoveChild(child); child.QueueFree(); }

		_subjectDeckWorking = SubjectDeck.Select(x => x).ToList();
		_modifierDeckWorking = ModifierDeck.Select(x => x).ToList();

		// Shuffle
		_subjectDeckWorking = _subjectDeckWorking.OrderBy(x => Random.Shared.Next()).ToList();
		_modifierDeckWorking = _modifierDeckWorking.OrderBy(x => Random.Shared.Next()).ToList();

		Hide();
		Round = 1;
		RoundMeter.NumRounds = MaxRound;
		
		void onTutorialComplete() {
			DialogueManager.Runner.TryDisconnect(DialogueRunner.SignalName.onDialogueComplete, onTutorialComplete);
			DialogueManager.Run("suitor_intro", true);
			Show();
			Deal();
			DialogueManager.Runner.TryConnect(DialogueRunner.SignalName.onDialogueComplete, new Callable(this, MethodName.OnDialogueComplete));
		}
		DialogueManager.Runner.TryConnect(DialogueRunner.SignalName.onDialogueComplete, onTutorialComplete);
		DialogueManager.Run("tutorial_intro");
	}

	public override void _Process(double delta) {
		PlayButton.Disabled = SubjectHand.GetSelected().Count() != 1 || ModifierHand.GetSelected().Count() != 1;
	}

	private void PlayHand() {
		var selectedSubject = SubjectHand.GetSelected().First();
		var selectedModifier = ModifierHand.GetSelected().First();

		var cardId = $"{selectedModifier.CardId}+{selectedSubject.CardId}";

		SubjectHand.RemoveChild(selectedSubject); selectedSubject.QueueFree();
		ModifierHand.RemoveChild(selectedModifier); selectedModifier.QueueFree();
		PlayButton.Hide();

		DialogueManager.Run(cardId);
	}

	private void OnDialogueComplete() {
		PlayButton.Show();

		Round += 1;

		if (Round <= MaxRound) { Deal(); }
		else { OnGameEnd(); }
	}

	private void OnGameEnd() {
		RoundMeter.Hide();
		SubjectHand.Hide();
		ModifierHand.Hide();
		PlayButton.Hide();
		DialogueManager.Runner.TryDisconnect(DialogueRunner.SignalName.onDialogueComplete, new Callable(this, MethodName.OnDialogueComplete));
		if (Score >= _loveThreshold) { DialogueManager.Run("love_ending", true); }
		else if (Score <= _hateThreshold) { DialogueManager.Run("hate_ending", true); }
		else { DialogueManager.Run("neutral_ending", true); }
		Callable.From(() => { 
			DialogueManager.Runner.TryConnect(DialogueRunner.SignalName.onDialogueComplete, new Callable(this, MethodName.OnGameEndDialogueComplete));
		}).CallDeferred();
	}

	private void OnGameEndDialogueComplete() {
		DialogueManager.Runner.TryDisconnect(DialogueRunner.SignalName.onDialogueComplete, new Callable(this, MethodName.OnGameEndDialogueComplete));
		var endScreen = ResourceLoader.Load<PackedScene>("res://scenes/ui/end_screen.tscn").Instantiate<EndScreen>();
		AddChild(endScreen);
		if (Score >= _loveThreshold) { endScreen.MessageLabel.Text = "Prepare for marriage."; }
		else if (Score <= _hateThreshold) { endScreen.MessageLabel.Text = "Prepare for war."; }
		else { endScreen.MessageLabel.Text = "You win!"; }	
	}

	private void Deal() {
		void InternalDeal(HandContainer container, List<string> workingDeck, Vector2 startPosition, float startAngle, double waitTime = 0.2) {
			int cardsToDeal = Math.Max(3 - container.GetChildCount(), 0);
			for (int i = 0; i < cardsToDeal; ++i) {
				var cardId = workingDeck.First();
				workingDeck.RemoveAt(0);
				var cardDisplay = new CardDisplay{ CardId = cardId, ShadowOffset = new(-10, 1), ShadowOpacity = 0.4f, GlobalPosition = startPosition, RotationDegrees = startAngle };
				Task.Factory.StartNew(async () => {
					await ToSignal(GetTree().CreateTimer(waitTime * i), Timer.SignalName.Timeout);
					container.AddChild(cardDisplay);
					cardDisplay.Owner = container;
				});
			}
		}
		InternalDeal(SubjectHand, _subjectDeckWorking, new Vector2(-200f, 100f), -30f);
		InternalDeal(ModifierHand, _modifierDeckWorking, new Vector2(500f, 100f), 30f);
	}
}
