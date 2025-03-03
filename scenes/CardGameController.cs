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

public partial class CardGameController : Control, IFocusContext
{
	public readonly int MaxRound = 8;
	public int Round { get; set { field = value; if (RoundMeter is not null) RoundMeter.CurrentRound = Round; if (Round > MaxRound) OnGameEnd(); } }

	[Export] public Godot.Collections.Dictionary<string, int> TopicDeck { get; set; }
	[Export] public Godot.Collections.Dictionary<string, int> ActionDeck { get; set; }

	private List<string> _topicDeckWorking, _actionDeckWorking;

	public ReadOnlyCollection<string> RemainingTopicDeck => _topicDeckWorking.AsReadOnly();
	public ReadOnlyCollection<string> RemainingActionDeck => _actionDeckWorking.AsReadOnly();

	[Export] public AffectionMeter AffectionMeter { get; set; }
	[Export] public HandContainer TopicHand { get; set; }
	[Export] public HandContainer ActionHand { get; set; }
	[Export] public Button PlayButton { get; set; }
	[Export] public RoundMeter RoundMeter { get; set; }
	
	public bool SkipIntro { get; set; }

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
		RoundMeter.NumRounds = MaxRound;

		PlayButton?.TryConnect(Button.SignalName.Pressed, new Callable(this, MethodName.PlayHand));
		var suitor = DialogueManager.GetCharacterDisplay("suitor");
		if (suitor is not null) { suitor.SpriteName = "neutral"; }

		foreach (var child in TopicHand.GetChildren()) { TopicHand.RemoveChild(child); child.QueueFree(); }
		foreach (var child in ActionHand.GetChildren()) { ActionHand.RemoveChild(child); child.QueueFree(); }

		List<string> CreateWorkingDeck(Godot.Collections.Dictionary<string, int> deck) {
			List<string> working = [];
			foreach (var (card, count) in deck) { for (int i = 0; i < count; ++i) { working.Add(card); } }
			return working;
		}

		// Create and shuffle deck
		_topicDeckWorking = CreateWorkingDeck(TopicDeck).OrderBy(x => Random.Shared.Next()).ToList();
		_actionDeckWorking = CreateWorkingDeck(ActionDeck).OrderBy(x => Random.Shared.Next()).ToList();

		Hide();
		Round = 1;
		
		void onTutorialComplete() {
			DialogueManager.Runner.TryDisconnect(DialogueRunner.SignalName.onDialogueComplete, onTutorialComplete);
			DialogueManager.Run("suitor_intro", true);
			Show();
			Deal();
			DialogueManager.Runner.TryConnect(DialogueRunner.SignalName.onDialogueComplete, new Callable(this, MethodName.OnDialogueComplete));
		}
		if (SkipIntro) { onTutorialComplete(); }
		else {
			DialogueManager.Runner.TryConnect(DialogueRunner.SignalName.onDialogueComplete, onTutorialComplete);
			DialogueManager.Run("tutorial_intro");
		}
	}

	public override void _Process(double delta) {
		PlayButton.Disabled = TopicHand.GetSelected().Count() != 1 || ActionHand.GetSelected().Count() != 1;
	}
	
    public Control GetDefaultFocus() => ActionHand;
    public Control GetDefaultFocus(InputManager.FocusDirection direction) => direction switch {
		_ when DialogueManager.Runner.IsDialogueRunning => null,
		InputManager.FocusDirection.Left => ActionHand.GetChildren().LastOrDefault() as Control,
		InputManager.FocusDirection.Right => TopicHand.GetChildren().FirstOrDefault() as Control,
		InputManager.FocusDirection.Up => PlayButton,
		_ => GetDefaultFocus()
	};

	private void PlayHand() {
		var selectedSubject = TopicHand.GetSelected().First();
		var selectedModifier = ActionHand.GetSelected().First();

		var cardId = $"{selectedModifier.CardId}+{selectedSubject.CardId}";

		TopicHand.RemoveChild(selectedSubject); selectedSubject.QueueFree();
		ActionHand.RemoveChild(selectedModifier); selectedModifier.QueueFree();
		PlayButton.Hide();

		DialogueManager.Run(cardId);
	}

	private void OnDialogueComplete() {
		PlayButton.Show();

		Round += 1;
		if (Round <= MaxRound) Deal();
	}

	private void OnGameEnd() {
		AffectionMeter.Hide();
		RoundMeter.Hide();
		TopicHand.Hide();
		ActionHand.Hide();
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
		InternalDeal(ActionHand, _actionDeckWorking, new Vector2(-200f, 100f), -30f);
		InternalDeal(TopicHand, _topicDeckWorking, new Vector2(500f, 100f), 30f);
	}
}
