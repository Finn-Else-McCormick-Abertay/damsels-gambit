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

public enum AffectionState { Neutral, Love, Hate }

[Tool]
public partial class CardGameController : Control, IReloadableToolScript, IFocusContext
{
	// Signals which are emitted on start and end of each phase
	[Signal] public delegate void IntroStartEventHandler(); 			[Signal] public delegate void IntroEndEventHandler();
	[Signal] public delegate void GameStartEventHandler(); 				[Signal] public delegate void GameEndEventHandler();
	[Signal] public delegate void RoundStartEventHandler(int round); 	[Signal] public delegate void RoundEndEventHandler(int round);
	
	public bool Started { get; private set; } = false;
	public bool Ended { get; private set; } = false;
	public bool MidRound { get; private set; } = false;
	
	// Current round. Triggers game end when it exceeds NumRounds
	public int Round { get; set { field = value; RoundMeter?.OnReady(x => x.CurrentRound = Round); AttemptGameEnd(); } }
	// Current score. When AutoFailOnHitThreshold is true, triggers game end when it exceeds score minimum or maximum
	public int Score { get; set { field = value; AffectionMeter?.CreateTween()?.TweenProperty(AffectionMeter, AffectionMeter.PropertyName.Value, int.Clamp(Score, ScoreMin, ScoreMax), 1.0); AttemptGameEnd(); } }

	// Current affection state based on score and thresholds
	public AffectionState AffectionState => Score switch { _ when Score >= LoveThreshold => AffectionState.Love, _ when Score <= HateThreshold => AffectionState.Hate, _ => AffectionState.Neutral };

	// Name of suitor. This converted to snake case will be used for generating dialogue node names, and for updating the profile
	[Export] public string SuitorName { get; set { field = value; _suitorId = Case.ToSnake(SuitorName); GameManager.NotebookMenu?.OnReady(x => x.SuitorName = SuitorName); } }
	private string _suitorId;
	
	// Should the skip intro button display? Set based on tags of intro node (cause it looks weird during an intro that's just a very short fade in)
	public bool IntroSkippable { get; private set; } = false;

	[Export(PropertyHint.Range, "0,20,")] public int NumRounds { get; private set { field = value; RoundMeter?.OnReady(() => RoundMeter.NumRounds = NumRounds); } } = 8;
	
	[ExportGroup("Score")]
	[Export(PropertyHint.Range, "-30,30,")] public int ScoreMin { get; private set { field = value; AffectionMeter?.OnReady(x => x.MinValue = ScoreMin); } } = -10;
	[Export(PropertyHint.Range, "-30,30,")] public int ScoreMax { get; private set { field = value; AffectionMeter?.OnReady(x => x.MaxValue = ScoreMax); } } = 10;

	[Export(PropertyHint.Range, "-30,30,")] public int LoveThreshold { get; private set { field = value; AffectionMeter?.OnReady(x => x.LoveThreshold = LoveThreshold); } } = 4;
	[Export(PropertyHint.Range, "-30,30,")] public int HateThreshold { get; private set { field = value; AffectionMeter?.OnReady(x => x.HateThreshold = HateThreshold); } } = -4;

	[Export] public bool AutoFailOnHitThreshold { get; set; } = true;

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

		PlayButton?.TryConnect(BaseButton.SignalName.Pressed, PlayHand);
		PlayButton.Disabled = true;

		// Remove any cards remaining from the editor
		foreach (var child in TopicHand.GetChildren()) { TopicHand.RemoveChild(child); child.QueueFree(); }
		foreach (var child in ActionHand.GetChildren()) { ActionHand.RemoveChild(child); child.QueueFree(); }
		
		Hide(); Round = 1;
	}

	// Trigger the start of the game
	// This doesn't happen in ready so that GameManager is able to set whether the intro should be skipped (which it is when loading into the scene directly)
	public void BeginGame(bool skipIntro = false) {
		// Creating list of cards from exported dictionary of cards and their counts
		static List<string> CreateWorkingDeck(Godot.Collections.Dictionary<string, int> deck) { List<string> working = []; foreach (var (card, count) in deck) for (int i = 0; i < count; ++i) working.Add(card); return working; }

		// Create and shuffle deck
		_topicDeckWorking = [..CreateWorkingDeck(TopicDeck).OrderBy(x => Random.Shared.Next())];
		_actionDeckWorking = [..CreateWorkingDeck(ActionDeck).OrderBy(x => Random.Shared.Next())];
		
		EmitSignal(SignalName.GameStart);

		// Make NotebookMenu the focus neighbour of topic hand, so it can be accessed via keyboard controls
		TopicHand.FocusNeighborTop = GameManager.NotebookMenu.GetPath();
		TopicHand.FocusNeighborRight = GameManager.NotebookMenu.GetPath();
		GameManager.NotebookMenu.FocusNeighborBottom = TopicHand.GetPath().ToString() + "!left";
		GameManager.NotebookMenu.FocusNeighborLeft = TopicHand.GetPath().ToString() + "!right";

		// Set whether intro skippable based on intro tags
		foreach (var tag in DialogueManager.Runner.GetTagsForNode($"{_suitorId}__intro")) {
			var args = tag.Split('=');
			if (tag.MatchN("skippable")) IntroSkippable = true; else if (tag.MatchN("unskippable")) IntroSkippable = false;
			else if (args.Length == 2 && args[0].Trim().ToLower().IsAnyOf([ "skip", "skippable" ]) && bool.TryParse(args[1].Trim(), out bool val)) IntroSkippable = val;
		}

		GameManager.NotebookMenu.Hide();
		
		// Run intro node or (skip_setup node, if skipping), then unhide and handle round start.
		// (Due to how AndThen works, if this dialogue is interrupted by another then the callback will still run when that dialogue ends, which is why force-running the skip setup (see ForceSkipIntro) doesn't break everything)
		DialogueManager.TryRun(skipIntro ? $"{_suitorId}__skip_setup" : $"{_suitorId}__intro")
			.AndThen(() => {
				Show(); GameManager.NotebookMenu.Show();
				Started = true; Round = 1;
				AttemptStartRound();
			});
	}

	public override void _ExitTree() {
		if (Engine.IsEditorHint()) return;
		GameManager.NotebookMenu.FocusNeighborBottom = new();
		GameManager.NotebookMenu.FocusNeighborLeft = new();
	}

	// If not yet started, force-run the skip_setup node, skipping to the start of gameplay even if currently in the intro
	public void ForceSkipIntro() { if (!Started && !Ended) DialogueManager.Run($"{_suitorId}__skip_setup", true); }

	public override void _Process(double delta) {
		if (Engine.IsEditorHint()) return;

		// Set play button to be disabled based on whether the correct number of cards are selected
		PlayButton.Disabled = TopicHand.GetSelected().Count() != 1 || ActionHand.GetSelected().Count() != 1 || ShouldGameEnd();
	}
	
	public Control GetDefaultFocus(FocusDirection direction) =>
		InputManager.FindFocusableWithin(direction switch {
			_ when DialogueManager.Runner.IsDialogueRunning => null,
			FocusDirection.Down => PlayButton,
			FocusDirection.Left => TopicHand,
			FocusDirection.Right or _ => ActionHand
		}, direction);

	// Connected to PlayButton's Pressed signal
	private void PlayHand() {
		if (Engine.IsEditorHint()) return;
		if (!Started) { Console.Warning("Attempted to play hand before game start."); return; }
		if (Ended) { Console.Warning("Attempted to play hand after game end."); return; }

		CardDisplay selectedTopic = TopicHand.GetSelected().SingleOrDefault();
		CardDisplay selectedAction = ActionHand.GetSelected().SingleOrDefault();

		// Verify that the correct number of cards were selected
		if (selectedTopic.IsInvalid() || selectedAction.IsInvalid()) { Console.Error("Failed to play hand. Topic: ", selectedTopic, ", Action: ", selectedAction); return; }

		var dialogueNode = $"{_suitorId}__{selectedAction.CardId.ToString().StripFront("action/")}_{selectedTopic.CardId.ToString().StripFront("topic/")}";

		TopicHand.RemoveChild(selectedTopic); selectedTopic.QueueFree();
		ActionHand.RemoveChild(selectedAction); selectedAction.QueueFree();
		PlayButton.Hide();

		// Run dialogue node, or error dialogue if it does not exist.
		// Then, emit round signal, increment round, and handle next round start
		DialogueManager.Run(dialogueNode)
			.AndThen(() => {
				MidRound = false;
				EmitSignal(SignalName.RoundEnd, Round);
				++Round; AttemptStartRound();
			});
	}

	// Are preconditions for game end met
	private bool ShouldGameEnd() => Started && !Ended && (Round > NumRounds || (AutoFailOnHitThreshold && !RangeOf<int>.Between(ScoreMin, ScoreMax).Contains(Score)));

	// Triggers round start so long as end preconditions are not met. Called by BeginGame and PlayHand
	private void AttemptStartRound() {
		if (MidRound) { Console.Warning("Failed to start round: previous round did not end."); return; }
		if (Engine.IsEditorHint() || ShouldGameEnd()) return;

		PlayButton.Show(); Deal();
		MidRound = true;
		EmitSignal(SignalName.RoundStart, Round);
	}

	// Triggers game end if the preconditions are met. Called by the Round and Score setters.
	private void AttemptGameEnd() {
		if (Engine.IsEditorHint() || !ShouldGameEnd()) return;

		// If mid-round, defer ending until end of round
		if (MidRound) { this.TryConnect(SignalName.RoundEnd, Callable.From((int round) => AttemptGameEnd()), (uint)ConnectFlags.OneShot); return; }

		PlayButton.Hide();

		CallableUtils.CallDeferred(() => {
			// Trigger the pre ending node if it exists (the tutorial uses this to run the profile explanation, so the UI can't be hidden yet).
			// Once it is finished (or if it didn't exist) then hide the UI and trigger the correct ending dialogue.
			// Once that is finished, emit the game end signal.
			DialogueManager.TryRun($"{_suitorId}__pre_ending")
				.AndThen(() => {
					AffectionMeter.Hide(); RoundMeter.Hide(); TopicHand.Hide(); ActionHand.Hide(); PlayButton.Hide();
					DialogueManager.TryRun($"{_suitorId}__ending__{AffectionState switch { AffectionState.Love => "love", AffectionState.Hate => "hate", AffectionState.Neutral => "neutral"}}")
						.AndThen(() => EmitSignal(SignalName.GameEnd));
				});
		});
	}

	// Deal up to each HandContainer's hand size, drawing from the working decks. Automatically handles tweens to animate them flying in from offscreen.
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

	// Equivalent of Deal, but deterministic and instant (for display in the editor)
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
