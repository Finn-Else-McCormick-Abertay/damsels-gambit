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

namespace DamselsGambit;

[Tool]
public partial class CardGameController : Control, IReloadableToolScript, IFocusContext
{
	[Signal] public delegate void GameStartEventHandler();
	[Signal] public delegate void GameEndEventHandler();
	[Signal] public delegate void RoundStartEventHandler(int round);
	[Signal] public delegate void RoundEndEventHandler(int round);

	[Export] public string SuitorName { get; set { field = value; _suitorId = Case.ToSnake(SuitorName); GameManager.NotebookMenu?.OnReady(x => x.SuitorName = SuitorName); } }
	private string _suitorId;

	[Export(PropertyHint.Range, "0,20,")] public int NumRounds { get; private set { field = value; RoundMeter?.OnReady(() => RoundMeter.NumRounds = NumRounds); } } = 8;

	public int Round { get; set { field = value;  RoundMeter?.OnReady(x => x.CurrentRound = Round); if (Round > NumRounds) TriggerGameEnd(); } }
	
	[ExportGroup("Score")]
	[Export(PropertyHint.Range, "-30,30,")] public int ScoreMin { get; private set { field = value; AffectionMeter?.OnReady(x => x.MinValue = ScoreMin); } } = -10;
	[Export(PropertyHint.Range, "-30,30,")] public int ScoreMax { get; private set { field = value; AffectionMeter?.OnReady(x => x.MaxValue = ScoreMax); } } = 10;

	[Export(PropertyHint.Range, "-30,30,")] public int LoveThreshold { get; private set { field = value; AffectionMeter?.OnReady(x => x.LoveThreshold = LoveThreshold); } } = 4;
	[Export(PropertyHint.Range, "-30,30,")] public int HateThreshold { get; private set { field = value; AffectionMeter?.OnReady(x => x.HateThreshold = HateThreshold); } } = -4;
	
	public int Score { get; set { field = value; AffectionMeter?.CreateTween()?.TweenProperty(AffectionMeter, AffectionMeter.PropertyName.Value.ToString(), Score, 1.0); } }

	[ExportGroup("Deck")]
	[Export] public Godot.Collections.Dictionary<string, int> TopicDeck { get; set { field = value; this.OnReady(EditorUpdateHands); } }
	[Export] public Godot.Collections.Dictionary<string, int> ActionDeck { get; set { field = value; this.OnReady(EditorUpdateHands); } }

	private List<string> _topicDeckWorking, _actionDeckWorking;

	public ReadOnlyCollection<string> RemainingTopicDeck => _topicDeckWorking?.AsReadOnly();
	public ReadOnlyCollection<string> RemainingActionDeck => _actionDeckWorking?.AsReadOnly();

	[ExportSubgroup("Draw")]
	[Export] public int ActionHandSize { get; set { field = value; ActionHand?.OnReady(x => x.HandSize = value); this.OnReady(EditorUpdateHands); } } = 3;
	[Export] public int TopicHandSize { get; set { field = value; TopicHand?.OnReady(x => x.HandSize = value); this.OnReady(EditorUpdateHands); } } = 3;
	[Export] public bool NoRepeatsInHand { get; set { field = value; this.OnReady(EditorUpdateHands); } } = false;

	[ExportGroup("Nodes")]
	[Export] public AffectionMeter AffectionMeter { get; set; }
	[Export] public RoundMeter RoundMeter { get; set; }
	[Export] public HandContainer ActionHand { get; set { field = value; this.OnReady(EditorUpdateHands); } }
	[Export] public HandContainer TopicHand { get; set { field = value; this.OnReady(EditorUpdateHands); } }
	[Export] public Button PlayButton { get; set; }

	public override void _Ready() {
		EditorUpdateHands();
		if (Engine.IsEditorHint()) return;

		PlayButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.PlayHand));
		PlayButton.Disabled = true;

		foreach (var child in TopicHand.GetChildren()) { TopicHand.RemoveChild(child); child.QueueFree(); }
		foreach (var child in ActionHand.GetChildren()) { ActionHand.RemoveChild(child); child.QueueFree(); }
		
		Hide(); Round = 1;
	}

	public bool Started { get; private set; } = false;
	public bool IntroSkippable { get; private set; } = false;

	public void BeginGame(bool skipIntro = false) {
		static List<string> CreateWorkingDeck(Godot.Collections.Dictionary<string, int> deck) { List<string> working = []; foreach (var (card, count) in deck) for (int i = 0; i < count; ++i) working.Add(card); return working; }

		// Create and shuffle deck
		_topicDeckWorking = [..CreateWorkingDeck(TopicDeck).OrderBy(x => Random.Shared.Next())];
		_actionDeckWorking = [..CreateWorkingDeck(ActionDeck).OrderBy(x => Random.Shared.Next())];
		
		EmitSignal(SignalName.GameStart);

		TopicHand.FocusNeighborTop = GameManager.NotebookMenu.GetPath();
		TopicHand.FocusNeighborRight = GameManager.NotebookMenu.GetPath();
		GameManager.NotebookMenu.FocusNeighborBottom = TopicHand.GetPath();
		GameManager.NotebookMenu.FocusNeighborLeft = TopicHand.GetPath();

		// Set intro skippable based on intro tags
		foreach (var tag in DialogueManager.Runner.GetTagsForNode($"{_suitorId}__intro")) {
			var args = tag.Split('=');
			if (tag.MatchN("skippable")) IntroSkippable = true; else if (tag.MatchN("unskippable")) IntroSkippable = false;
			else if (args.Length == 2 && args[0].Trim().ToLower().IsAnyOf([ "skip", "skippable" ]) && bool.TryParse(args[1].Trim(), out bool val)) IntroSkippable = val;
		}
		
		DialogueManager.TryRun(skipIntro ? $"{_suitorId}__skip_setup" : $"{_suitorId}__intro").AndThen(() => {
			Started = true;
			Round = 1; Show(); Deal();
			EmitSignal(SignalName.RoundStart, Round);
		});
	}

	public void ForceSkipIntro() { if (!Started) DialogueManager.Run($"{_suitorId}__skip_setup", true); }

	public override void _Process(double delta) {
		if (Engine.IsEditorHint()) return;

		PlayButton.Disabled = TopicHand.GetSelected().Count() != 1 || ActionHand.GetSelected().Count() != 1;
	}
	
	public Control GetDefaultFocus(FocusDirection direction) =>
		InputManager.FindFocusableWithin(direction switch {
			_ when DialogueManager.Runner.IsDialogueRunning => null,
			FocusDirection.Down => PlayButton,
			FocusDirection.Left => TopicHand,
			FocusDirection.Right or _ => ActionHand
		}, direction);

	private void PlayHand() {
		if (Engine.IsEditorHint()) return;

		CardDisplay selectedTopic = TopicHand.GetSelected().First(), selectedAction = ActionHand.GetSelected().First();

		var dialogueNode = $"{_suitorId}__{selectedAction.CardId.ToString().StripFront("action/")}_{selectedTopic.CardId.ToString().StripFront("topic/")}";

		TopicHand.RemoveChild(selectedTopic); selectedTopic.QueueFree();
		ActionHand.RemoveChild(selectedAction); selectedAction.QueueFree();
		PlayButton.Hide();

		DialogueManager.Run(dialogueNode).AndThen(() => {
			EmitSignal(SignalName.RoundEnd, Round);
			Round += 1;
			if (Round <= NumRounds) { PlayButton.Show(); Deal(); EmitSignal(SignalName.RoundStart, Round); }
		});
	}

	private void TriggerGameEnd() {
		if (Engine.IsEditorHint()) return;

		PlayButton.Hide();

		DialogueManager
			.TryRun($"{_suitorId}__pre_ending")
			.AndThen(() => {
				AffectionMeter.Hide(); RoundMeter.Hide(); TopicHand.Hide(); ActionHand.Hide();
				DialogueManager
					.TryRun($"{_suitorId}__ending__{Score switch { _ when Score >= LoveThreshold => "love", _ when Score <= HateThreshold => "hate", _ => "neutral" }}")
					.AndThen(() => EmitSignal(SignalName.GameEnd));
			});
	}

	private void Deal() {
		if (Engine.IsEditorHint()) return;

		void DealToHand(HandContainer container, int handSize, List<string> workingDeck, Vector2 startPosition, float startAngle, double waitTime = 0.2) {
			int cardsToDeal = Math.Max(handSize - container.GetChildCount(), 0);
			List<string> cardsInHand = [..container.FindChildrenOfType<CardDisplay>().Select(x => x.CardId.ToString())];
			for (int i = 0; i < cardsToDeal; ++i) {
				var cardId = (NoRepeatsInHand ? workingDeck.SkipWhile(cardsInHand.Contains) : workingDeck).FirstOrDefault() ?? workingDeck.FirstOrDefault();
				if (cardId is null) break;
				cardsInHand.Add(cardId); workingDeck.Remove(cardId);
				var cardDisplay = new CardDisplay{ CardId = cardId, ShadowOffset = new(-10, 1), ShadowOpacity = 0.4f, GlobalPosition = startPosition, RotationDegrees = startAngle };
				Task.Factory.StartNew(async () => {
					await ToSignal(GetTree().CreateTimer(waitTime * i), Timer.SignalName.Timeout);
					container.AddChild(cardDisplay);
					cardDisplay.Owner = container;
				});
			}
		}
		DealToHand(ActionHand, ActionHandSize, _actionDeckWorking, new Vector2(-200f, 100f), -30f);
		DealToHand(TopicHand, TopicHandSize, _topicDeckWorking, new Vector2(500f, 100f), 30f);
	}

	private void EditorUpdateHands() {
		if (!Engine.IsEditorHint()) return;

		var random = new Random(0);
		
		List<string> CreateConsistentShuffledDeck(Godot.Collections.Dictionary<string, int> deck) {
			List<string> working = []; foreach (var (card, count) in deck) for (int i = 0; i < count; ++i) working.Add(card);
			return [..working.OrderBy(x => random.Next())];
		}

		void UpdateHand(HandContainer container, int handSize, Godot.Collections.Dictionary<string, int> deck) {
			if (!container.IsValid() || deck is null) return;
			
			foreach (var child in container.GetChildren()) { container.RemoveChild(child); child.QueueFree(); }

			var shuffled = CreateConsistentShuffledDeck(deck);
			for (int i = 0; i < handSize; ++i) {
				var cardId = (NoRepeatsInHand ? shuffled.SkipWhile(id => container.FindChildrenWhere<CardDisplay>(card => card.CardId == id).Count > 0) : shuffled).FirstOrDefault();
				if (cardId is null) break;
				shuffled.Remove(cardId);
				var cardDisplay = new CardDisplay{ CardId = cardId, ShadowOffset = new(-10, 1), ShadowOpacity = 0.4f };
				container.AddChild(cardDisplay);
			}
		}

		UpdateHand(ActionHand, ActionHandSize, ActionDeck);
		UpdateHand(TopicHand, TopicHandSize, TopicDeck);
	}
}
