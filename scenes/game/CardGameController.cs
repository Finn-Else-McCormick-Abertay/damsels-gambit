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
using Bridge;

namespace DamselsGambit;

public enum AffectionState { Neutral, Love, Hate }

[Tool]
public partial class CardGameController : Control, IReloadableToolScript, IFocusContext
{
	// Signals which are emitted on start and end of each phase
	[Signal] public delegate void IntroStartEventHandler(); 			[Signal] public delegate void IntroEndEventHandler();
	[Signal] public delegate void GameStartEventHandler(); 				[Signal] public delegate void GameEndEventHandler();
	[Signal] public delegate void RoundStartEventHandler(int round); 	[Signal] public delegate void RoundEndEventHandler(int round);

	[Signal] public delegate void HandPlayedEventHandler(StringName actionCard, StringName topicCard);	[Signal] public delegate void DiscardedEventHandler(StringName[] cards);
	[Signal] public delegate void DeckRanOutEventHandler();
	
	private bool _initialised = false;
	public bool Started { get; private set; }	public bool Ended { get; private set; }
	public bool MidRound { get; private set; }	public bool InDialogue { get; private set; }
	
	// Current round and score. Setters update relevant meters and (if conditions are met) trigger game end
	public int Round { get; set { field = value; RoundMeter?.OnReady(x => x.CurrentRound = value); AttemptGameEnd(); } }
	public int Score { get; set { AnimateScoreEffect(field, value); field = value; AffectionMeter?.OnReady(x => x.Value = value); AttemptGameEnd(); } }

	// Current affection state based on score and thresholds
	public AffectionState AffectionState => Score switch { _ when Score > LoveThreshold => AffectionState.Love, _ when Score < HateThreshold => AffectionState.Hate, _ => AffectionState.Neutral };

	// Name of suitor. This converted to snake case will be used for generating dialogue node names, and for updating the profile
	[Export] public string SuitorName { get; set { field = value; _suitorId = Case.ToSnake(SuitorName); GameManager.NotebookMenu?.OnReady(x => x.SuitorName = SuitorName); } }
	private string _suitorId;
	
	// Should the skip intro button display? Set based on tags of intro node (cause it looks weird during an intro that's just a very short fade in)
	public bool IntroSkippable { get; private set; } = false;

	[ExportCategory("Rounds")]
	[Export(PropertyHint.Range, "0,20,")] public int NumRounds { get; private set { field = value; this.OnReady(() => RoundMeter?.OnReady(x => x.NumRounds = NumRounds)); } } = 8;
	[Export] private bool QuestionsTriggerRoundEnd { get; set; } = false;
	
	[ExportGroup("Score")]
	[Export(PropertyHint.Range, "-30,30,")] public int ScoreMin { get; private set { field = value; AffectionMeter?.OnReady(x => x.MinValue = value); } } = -10;
	[Export(PropertyHint.Range, "-30,30,")] public int ScoreMax { get; private set { field = value; AffectionMeter?.OnReady(x => x.MaxValue = value); } } = 10;

	[Export(PropertyHint.Range, "-30,30,")] public int LoveThreshold { get; private set { field = value; AffectionMeter?.OnReady(x => x.LoveThreshold = value); } } = 4;
	[Export(PropertyHint.Range, "-30,30,")] public int HateThreshold { get; private set { field = value; AffectionMeter?.OnReady(x => x.HateThreshold = value); } } = -4;

	[Export] public bool AutoFailOnHitThreshold { get; set; } = true;

	[ExportGroup("Deck", "FullLayout")]
	[Export] public Godot.Collections.Dictionary<string, int> FullLayoutTopicDeck { get; set { field = value; this.OnReady(EditorUpdateHands); } }
	[Export] public Godot.Collections.Dictionary<string, int> FullLayoutActionDeck { get; set { field = value; this.OnReady(EditorUpdateHands); } }
	
	public Deck TopicDeck { get; private set; } public Deck ActionDeck { get; private set; }
	private Deck _topicDiscardPile, _actionDiscardPile;
	
	[ExportGroup("Draw")]
	[Export] public int ActionHandSize { get; set { field = value; ActionHand?.OnReady(x => { x.HandSize = value; EditorUpdateHands(); }); } } = 3;
	[Export] public int TopicHandSize { get; set { field = value; TopicHand?.OnReady(x => { x.HandSize = value; EditorUpdateHands(); }); } } = 3;
	[Export] public bool NoRepeatsInHand { get; set { field = value; this.OnReady(EditorUpdateHands); } }
	[Export] public bool SendPlayedCardsToDiscardPile { get; set; }
	[Export] public bool ReshuffleDiscardPileOnDeckRunOut { get; set; } = true;
	
	[ExportGroup("Discard")]
	[Export] public int DiscardLimitPerGame { get; set; } = -1;
	[Export] public int DiscardLimitPerRound { get; set; } = 1;
	[Export] public int MaxCardsPerDiscard { get; set; } = -1;
	[Export] public bool DiscardTriggersRoundEnd { get; set; } = false;

	[ExportSubgroup("Dialogue")]
	[Export(PropertyHint.Range, "0,1,")] public double DiscardDialogueTriggerChance { get; set; } = 1;
	[Export] public bool SkipAlreadySeenDiscardDialogues { get; set; } = true;
	[Export] public bool DiscardOnlyEndsRoundOnDialogueHit { get; set; } = false;	

	public int UsedDiscardsThisGame { get; private set; }
	public int UsedDiscardsThisRound { get; private set; }
	private readonly HashSet<string> _usedDiscardDialogues = [];

	[ExportGroup("Animation")]
	[Export] public double MeterSlideInTime { get; set; } = 0.5;
	[Export] public double CardFallTime { get; set; } = 0.5;

	[Export] public PackedScene PositiveEffectScene { get; set; }
	[Export] public PackedScene NegativeEffectScene { get; set; }

	[ExportGroup("Nodes")]
	[Export] public AffectionMeter AffectionMeter { get; set; } 												[Export] public RoundMeter RoundMeter { get; set; }
	[Export] public HandContainer ActionHand { get; set { field = value; this.OnReady(EditorUpdateHands); } }	[Export] public HandContainer TopicHand { get; set { field = value; this.OnReady(EditorUpdateHands); } }
	[Export] public Button PlayButton { get; set; } 															[Export] public Button DiscardButton { get; set; }

	public class Deck
	{
		private List<string> _working;

		public Deck() => _working = [];
		public Deck(IEnumerable<string> cards) => _working = [..cards];
		public Deck(Dictionary<string, int> layout) { _working = []; foreach (var (card, count) in layout) for (int i = 0; i < count; ++i) _working.Add(card); }
		public Deck(Godot.Collections.Dictionary<string, int> layout) : this(layout.ToDictionary()) {}

		public static Deck Copy(Deck otherDeck) { Deck newDeck = new(); newDeck._working.AddRange(otherDeck._working); return newDeck; }

		public ReadOnlyCollection<string> Remaining => _working.AsReadOnly();

		public string Peek() => _working.FirstOrDefault();
		public string PeekWhere(Func<string, bool> predicate) => _working.FirstOrDefault(predicate);

		public string Draw() { var card = _working.FirstOrDefault(); if (card is not null) _working.Remove(card); return card; }
		public string DrawWhere(Func<string, bool> predicate) { var card = _working.FirstOrDefault(predicate); if (card is not null) _working.Remove(card); return card; }

		public void Add(string card) => _working.Add(card);
		public void Add(IEnumerable<string> cards) => _working.AddRange(cards);
		public void Add(Deck deck) => _working.AddRange(deck._working);
		public void Clear() => _working.Clear();

		public bool IsEmpty() => _working.Count == 0;

		public void Shuffle(Random random = null) => _working = [.._working.OrderBy(x => (random ?? Random.Shared).Next())];
	}
	
	private enum GameVisibilityState { AllVisible, AllHidden, ButtonsHidden, AllHiddenInstant }

	private Tween _meterTween;
	private void AnimateMetersIn() {
		if (GodotObject.IsInstanceValid(_meterTween)) _meterTween.Kill();
		_meterTween = CreateTween();
		float affectionMeterDistance = AffectionMeter.Position.X + AffectionMeter.Size.X + 10f; float roundMeterDistance = RoundMeter.Position.Y + RoundMeter.Size.Y + 20f;
		_meterTween.TweenProperty(AffectionMeter, "position:x", -affectionMeterDistance, 0).AsRelative(); _meterTween.TweenProperty(RoundMeter, "position:y", -roundMeterDistance, 0).AsRelative();
		_meterTween.TweenCallback(AffectionMeter.Show); _meterTween.TweenCallback(RoundMeter.Show);
		_meterTween.TweenProperty(AffectionMeter, "position:x", affectionMeterDistance, MeterSlideInTime).AsRelative(); _meterTween.Parallel().TweenProperty(RoundMeter, "position:y", roundMeterDistance, MeterSlideInTime).AsRelative();

	}
	private void AnimateMetersOut() {
		if (GodotObject.IsInstanceValid(_meterTween)) _meterTween.Kill();
		_meterTween = CreateTween();
		float affectionMeterDistance = AffectionMeter.Position.X + AffectionMeter.Size.X + 10f; float roundMeterDistance = RoundMeter.Position.Y + RoundMeter.Size.Y + 20f;
		_meterTween.TweenProperty(AffectionMeter, "position:x", -affectionMeterDistance, MeterSlideInTime).AsRelative(); _meterTween.Parallel().TweenProperty(RoundMeter, "position:y", -roundMeterDistance, MeterSlideInTime).AsRelative();
		_meterTween.TweenCallback(AffectionMeter.Hide); _meterTween.TweenCallback(RoundMeter.Hide);
		_meterTween.TweenProperty(AffectionMeter, "position:x", affectionMeterDistance, 0).AsRelative(); _meterTween.TweenProperty(RoundMeter, "position:y", roundMeterDistance, 0).AsRelative();
	}

	private GameVisibilityState VisibilityState {
		get;
		set {
			var oldState = field;
			field = value;
			if (Engine.IsEditorHint()) return;
			switch (VisibilityState) {
				case GameVisibilityState.AllVisible: {
					TopicHand.Show(); ActionHand.Show();
					PlayButton.Show(); DiscardButton.Visible = DiscardLimitPerGame != 0 && DiscardLimitPerRound != 0 && MaxCardsPerDiscard != 0;
					GameManager.NotebookMenu.Show();
					if (!oldState.IsAnyOf(GameVisibilityState.AllVisible, GameVisibilityState.ButtonsHidden)) AnimateMetersIn();
				} break;
				case GameVisibilityState.ButtonsHidden: {
					TopicHand.Show(); ActionHand.Show();
					PlayButton.Hide(); DiscardButton.Hide();
					GameManager.NotebookMenu.Show();
					if (!oldState.IsAnyOf(GameVisibilityState.AllVisible, GameVisibilityState.ButtonsHidden)) AnimateMetersIn();
				} break;
				case GameVisibilityState.AllHidden: {
					TopicHand.Hide(); ActionHand.Hide();
					PlayButton.Hide(); DiscardButton.Hide();
					GameManager.NotebookMenu.Hide();
					if (_initialised && oldState != GameVisibilityState.AllHidden) AnimateMetersOut(); else { AffectionMeter.Hide(); RoundMeter.Hide(); }
				} break;
				default: break;
			}
		}
	}

	public override void _Ready() {
		EditorUpdateHands();
		if (Engine.IsEditorHint()) return;

		if (PlayButton.IsValid()) { PlayButton.TryConnect(BaseButton.SignalName.Pressed, AttemptPlayHand); PlayButton.Disabled = true; }
		if (DiscardButton.IsValid()) { DiscardButton.TryConnect(BaseButton.SignalName.Pressed, AttemptDiscard); DiscardButton.Disabled = true; }

		InputManager.Actions.Play.InnerObject.Connect(GUIDEAction.SignalName.Completed, AttemptPlayHand);
		InputManager.Actions.Discard.InnerObject.Connect(GUIDEAction.SignalName.Completed, AttemptDiscard);

		// Remove any cards remaining from the editor
		foreach (var child in TopicHand.GetChildren().Concat(ActionHand.GetChildren())) { child.GetParent().RemoveChild(child); child.QueueFree(); }
		
		VisibilityState = GameVisibilityState.AllHidden;
		_initialised = true; Round = 1;
	}

	// Trigger the start of the game
	// This doesn't happen in ready so that GameManager is able to set whether the intro should be skipped (which it is when loading into the scene directly)
	public void BeginGame(bool skipIntro = false) {
		TopicDeck = new(FullLayoutTopicDeck); ActionDeck = new(FullLayoutActionDeck); // Create decks from exported deck layout dictionaries
		TopicDeck.Shuffle(); ActionDeck.Shuffle(); // Shuffle decks

		_topicDiscardPile = new(); _actionDiscardPile = new(); // Create empty discard piles
		_usedDiscardDialogues.Clear();
		UsedDiscardsThisGame = 0; UsedDiscardsThisRound = 0;

		// Make NotebookMenu the focus neighbour of topic hand, so it can be accessed via keyboard controls
		TopicHand.FocusNeighborTop = GameManager.NotebookMenu.GetPath(); TopicHand.FocusNeighborRight = GameManager.NotebookMenu.GetPath();
		GameManager.NotebookMenu.FocusNeighborBottom = TopicHand.GetPath().ToString() + "$from right"; GameManager.NotebookMenu.FocusNeighborLeft = TopicHand.GetPath().ToString() + "$from left";

		// Set whether intro skippable based on intro tags
		foreach (var tag in DialogueManager.Runner.GetTagsForNode($"{_suitorId}__intro")) {
			var args = tag.Split('=');
			if (tag.MatchN("skippable")) IntroSkippable = true; else if (tag.MatchN("unskippable")) IntroSkippable = false;
			else if (args.Length == 2 && args[0].Trim().ToLower().IsAnyOf([ "skip", "skippable" ]) && bool.TryParse(args[1].Trim(), out bool val)) IntroSkippable = val;
		}
		
		EmitSignal(SignalName.GameStart);
		VisibilityState = GameVisibilityState.AllHidden;
		
		// Run intro node or (skip_setup node, if skipping), then unhide and handle round start.
		// (Due to how AndThen works, if this dialogue is interrupted by another then the callback will still run when that dialogue ends, which is why force-running the skip setup (see ForceSkipIntro) doesn't break everything)
		InDialogue = true;
		DialogueManager.TryRun(skipIntro ? $"{_suitorId}__skip_setup" : $"{_suitorId}__intro")
			.AndThen(() => {
				InDialogue = false;
				VisibilityState = GameVisibilityState.AllVisible;
				Started = true; Round = 1; AttemptStartRound();
			});
	}

	public override void _ExitTree() {
		if (Engine.IsEditorHint()) return;
		GameManager.NotebookMenu.FocusNeighborBottom = new(); GameManager.NotebookMenu.FocusNeighborLeft = new();
	}
	
	public override void _Process(double delta) {
		if (Engine.IsEditorHint()) return;

		// Set play button to be disabled based on whether the correct number of cards are selected
		if (PlayButton.IsValid()) PlayButton.Disabled = !CanPlay || ShouldGameEnd;
		// Set discard button to be disabled based on whether any cards are selected
		if (DiscardButton.IsValid()) DiscardButton.Disabled = !CanDiscard || ShouldGameEnd;
	}
	
	public Control GetDefaultFocus(FocusDirection direction) =>
		InputManager.FindFocusableWithin(direction switch {
			_ when DialogueManager.Runner.IsDialogueRunning => null,
			FocusDirection.Down => PlayButton,
			FocusDirection.Left => TopicHand,
			FocusDirection.Right or _ => ActionHand
		}, direction);

	// Are preconditions for playing hand met (are correct number of cards selected?)
	private bool CanPlay => Started && !Ended && !InDialogue && TopicHand.GetSelected().Count() == 1 && ActionHand.GetSelected().Count() == 1;

	// Are preconditions for discarding met
	private bool CanDiscard => Started && !Ended && !InDialogue &&
		(TopicHand.GetSelected().Any() || ActionHand.GetSelected().Any())
		&& (DiscardLimitPerGame < 0 || UsedDiscardsThisGame < DiscardLimitPerGame)
		&& (DiscardLimitPerRound < 0 || UsedDiscardsThisRound < DiscardLimitPerRound);

	// Are preconditions for game end met
	private bool ShouldGameEnd => Started && !Ended && (Round > NumRounds || (AutoFailOnHitThreshold && !RangeOf<int>.Over(ScoreMin, ScoreMax).Contains(Score)));
	
	// If not yet started, force-run the skip_setup node, skipping to the start of gameplay even if currently in the intro
	public void ForceSkipIntro() { if (!Started && !Ended) DialogueManager.Run($"{_suitorId}__skip_setup", true); }

	// Force-end the game without deferring until end of round. Used by debug commands
	public void ForceGameEnd() { Round = NumRounds + 1; if (MidRound) AttemptGameEnd(true); }

	private void AnimateCardRemoval(CardDisplay card) {
		card.Reparent(this); var tween = CreateTween();
		tween.TweenProperty(card, "position", new Vector2(5, 350), CardFallTime).AsRelative();
		tween.Parallel().TweenProperty(card, "rotation_degrees", 20, CardFallTime).AsRelative();
		tween.TweenCallback(card.QueueFree);
	}

	private Node2D _scoreEffect = null;

	private void AnimateScoreEffect(int oldScore, int newScore) {
		if (oldScore == newScore) return;
		if (_scoreEffect.IsValid()) _scoreEffect.QueueFree();
		_scoreEffect = ((newScore - oldScore) switch { > 0 => PositiveEffectScene, _ => NegativeEffectScene })?.Instantiate<Node2D>();
		AddChild(_scoreEffect);

		var animationPlayer = _scoreEffect.FindChildOfType<AnimationPlayer>();
		var animationName = animationPlayer.GetAnimationList().FirstOrDefault(x => x != "RESET");
		animationPlayer.Play(animationName);
	}

	// Plays hand so long as end preconditions are not met. Connected to PlayButton's Pressed signal.
	private void AttemptPlayHand() {
		if (Engine.IsEditorHint()) return;
		if (!CanPlay) return; // Verify that the attempt is valid

		CardDisplay selectedTopic = TopicHand.GetSelected().Single(), selectedAction = ActionHand.GetSelected().Single();

		// Remove cards from hand
		void RemoveFromHand(CardDisplay card) { if (SendPlayedCardsToDiscardPile) (card.CardType switch { "action" => _actionDiscardPile, "topic" => _topicDiscardPile, _ => null })?.Add(card.CardId); AnimateCardRemoval(card); }
		RemoveFromHand(selectedAction); RemoveFromHand(selectedTopic);
		
		EmitSignal(SignalName.HandPlayed, selectedAction.CardId, selectedTopic.CardId);
		VisibilityState = GameVisibilityState.ButtonsHidden; 

		// Get dialogue node name for given cards
		var dialogueNode = $"{_suitorId}__{selectedAction.CardId.ToString().StripFront("action/")}_{selectedTopic.CardId.ToString().StripFront("topic/")}";
		bool shouldEndRound = QuestionsTriggerRoundEnd || selectedAction.CardId != "action/question";

		// Run dialogue node, or error dialogue if it does not exist.
		// Then, emit round signal, increment round, and handle next round start
		InDialogue = true;
		DialogueManager.Run(dialogueNode)
			.AndThen(() => {
				InDialogue = false;
				if (shouldEndRound) { MidRound = false; EmitSignal(SignalName.RoundEnd, Round); ++Round; AttemptStartRound(); }
				else { VisibilityState = GameVisibilityState.AllVisible; Deal(); }
			});
	}

	// Discards selected cards so long as end preconditions are not met. Connected to DiscardButton's pressed signal.
	private void AttemptDiscard() {
		if (Engine.IsEditorHint()) return;
		if (!CanDiscard) return; // Verify that the attempt is valid

		UsedDiscardsThisGame++; UsedDiscardsThisRound++;
		var cardsDiscarded = TopicHand.GetSelected().Concat(ActionHand.GetSelected()).Select(x => x.CardId).Select(x => x.ToString()).ToArray();

		void DiscardSelected(HandContainer hand) => hand.GetSelected().ForEach(card => { (card.CardType switch { "action" => _actionDiscardPile, "topic" => _topicDiscardPile, _ => null })?.Add(card.CardId); AnimateCardRemoval(card); });
		DiscardSelected(ActionHand); DiscardSelected(TopicHand);

		void AfterDiscard(bool dialogueHit) { if (DiscardTriggersRoundEnd && (dialogueHit || !DiscardOnlyEndsRoundOnDialogueHit)) { MidRound = false; ++Round; AttemptStartRound(); } else Deal(); }

		if (DiscardDialogueTriggerChance switch { <= 0 => false, >= 1 => true, _ => Random.Shared.NextDouble() < DiscardDialogueTriggerChance }) {
			// Will pick at random from all nodes starting with '{suitor}__post_discard'
			// If the nodes have tags in the format 'action={card1},{card2}' or 'topic={card}' etc, then that node will only be pickable if one of the listed cards was just discarded
			// (If the card name starts with a !, it will instead disqualify the node if that card was just discarded)
			// If no node can be selected, it will try '{suitor}__post_discard_fallback'. The fallback can appear multiple times, even if SkipAlreadySeenDiscardDialogues is true.
			// If no node can resolve at all, it will simply skip past the dialogue step.

			var variants = DialogueManager.GetNodeNames().Where(x => x.StartsWith($"{_suitorId}__post_discard")).Where(x => x != $"{_suitorId}__post_discard_fallback");
			if (SkipAlreadySeenDiscardDialogues) variants = variants.Where(x => !_usedDiscardDialogues.Contains(x));

			List<string> highPriorityVariants = [];
			variants = variants.Where(node => {
				var tags = DialogueManager.Runner.GetTagsForNode(node);
				List<string> allowedCards = [], disallowedCards = [];
				bool isHighPriority = false;
				foreach (var tag in tags) {
					if (tag.Contains('=')) {
						var equalsSplit = tag.Split('=', 2, StringSplitOptions.TrimEntries);
						string cardType = equalsSplit.First().ToLower();
						string[] args = equalsSplit.Last().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
						allowedCards.AddRange(args.Where(x => !x.StartsWith('!')).Select(x => $"{cardType}/{x.ToLower()}"));
						disallowedCards.AddRange(args.Where(x => x.StartsWith('!')).Select(x => $"{cardType}/{x.StripFront('!').ToLower()}"));
						if (allowedCards.Count == 0 && disallowedCards.Count == 0) continue;
						foreach (var disallowedCard in disallowedCards) { if (cardsDiscarded.Contains(disallowedCard)) return false; }
						if (allowedCards.Count > 0) {
							bool anyAllowed = false;
							foreach (var allowedCard in allowedCards) anyAllowed = anyAllowed || cardsDiscarded.Contains(allowedCard);
							if (!anyAllowed) return false;
						}
					}
					if (Case.ToSnake(tag) == "high_priority") isHighPriority = true;
				}
				//Console.Info($"Node: {node}, AllowedCards: [{string.Join(", ", allowedCards)}], DisallowedCards: [{string.Join(", ", disallowedCards)}]{isHighPriority switch { true => ", HighPriority", false => "" }}");
				if (disallowedCards.Any(cardsDiscarded.Contains) || (allowedCards.Count > 0 && !allowedCards.Any(cardsDiscarded.Contains))) return false;
				if (isHighPriority) highPriorityVariants.Add(node);
				return true;
			});

			//Console.Info($"Variants: {variants.ToPrettyString()}, High Priority: {highPriorityVariants.ToPrettyString()}");

			// If some possible nodes are denoted as high priority, select from only them
			if (highPriorityVariants.Count > 0) variants = highPriorityVariants;

			// Pick at random from possible options
			variants = variants.OrderBy(x => Random.Shared.Next());
			string dialogueNode = variants.FirstOrDefault();

			if (dialogueNode is null && DialogueManager.DialogueExists($"{_suitorId}__post_discard_fallback")) dialogueNode = $"{_suitorId}__post_discard_fallback";

			if (dialogueNode is not null) {
				_usedDiscardDialogues.Add(dialogueNode);
				InDialogue = true;
				
				VisibilityState = GameVisibilityState.ButtonsHidden;
				DialogueManager.TryRun(dialogueNode)
					.AndThen(() => {
						InDialogue = false;
						VisibilityState = GameVisibilityState.AllVisible;
						AfterDiscard(true);
					});
			}
			else AfterDiscard(false);
		}
		else AfterDiscard(false);
	}

	// Triggers round start so long as end preconditions are not met. Called by BeginGame and PlayHand
	private void AttemptStartRound() {
		if (MidRound) { Console.Warning("Failed to start round: previous round did not end."); return; }
		if (Engine.IsEditorHint() || ShouldGameEnd) return;

		UsedDiscardsThisRound = 0;
		VisibilityState = GameVisibilityState.AllVisible;
		MidRound = true;
		Deal();
		
		EmitSignal(SignalName.RoundStart, Round);
	}

	// Triggers game end if the preconditions are met. Called by the Round and Score setters.
	private void AttemptGameEnd(bool force = false) {
		if (Engine.IsEditorHint() || !ShouldGameEnd) return;

		// If mid-round, defer ending until end of round
		if (MidRound && !force) { this.TryConnect(SignalName.RoundEnd, Callable.From((int round) => AttemptGameEnd()), (uint)ConnectFlags.OneShot); return; }


		
		Ended = true;
		VisibilityState = GameVisibilityState.ButtonsHidden;

		CallableUtils.CallDeferred(() => {
			// Trigger the pre ending node if it exists (the tutorial uses this to run the profile explanation, so the UI can't be hidden yet).
			// Once it is finished (or if it didn't exist) then hide the UI and trigger the correct ending dialogue.
			// Once that is finished, emit the game end signal.
			InDialogue = true;
			VisibilityState = GameVisibilityState.ButtonsHidden;
			DialogueManager.TryRun($"{_suitorId}__pre_ending")
				.AndThen(() => {
					InDialogue = false;
					VisibilityState = GameVisibilityState.AllHidden;

					// Only trigger the end game audio if the date is with the Prince.
					if (_suitorId == "frostholm")
					{
						AudioManager.StopMusic();
						AudioManager.PlayMusic(AffectionState switch { AffectionState.Love => "res://assets/audio/MarryEnd.mp3",AffectionState.Hate=> "res://assets/audio/WarEnd.mp3", AffectionState.Neutral => "res://assets/audio/GoodEnd.mp3"});

					}
					
					DialogueManager.TryRun($"{_suitorId}__ending__{AffectionState switch { AffectionState.Love => "love", AffectionState.Hate => "hate", AffectionState.Neutral => "neutral"}}")
						.AndThen(() => EmitSignal(SignalName.GameEnd));
				});
		});
	}

	// Deal up to each HandContainer's hand size, drawing from the working decks. Automatically handles tweens to animate them flying in from offscreen.
	private void Deal() {
		if (Engine.IsEditorHint()) return;

		void DealToHand(HandContainer container, int handSize, Deck deck, Deck discardPile, Vector2 startPosition, float startAngle, double waitTime = 0.2) {
			int cardsToDeal = Math.Max(handSize - container.GetChildCount(), 0);
			List<string> cardsInHand = [..container.FindChildrenOfType<CardDisplay>().Select(x => x.CardId.ToString())];
			foreach (var i in RangeOf<int>.UpTo(cardsToDeal)) {
				string cardId = null;
				int drawAttempts = 0;
				while (cardId is null) {
					drawAttempts++;
					cardId = NoRepeatsInHand ? deck.DrawWhere(id => !cardsInHand.Contains(id)) : deck.Draw();
					if (cardId is null) {
						EmitSignal(SignalName.DeckRanOut);
						// Reshuffle discard pile back into deck upon running out of cards
						if (ReshuffleDiscardPileOnDeckRunOut) { deck.Add(discardPile); discardPile.Clear(); deck.Shuffle(); }
						else break;
					}
					if (drawAttempts >= 20) break;
				}
				if (cardId is null) { Console.Error($"Failed to deal card from deck [{string.Join(", ", deck.Remaining)}] in {drawAttempts} attempts."); continue; }
				cardsInHand.Add(cardId);
				var cardDisplay = new CardDisplay{ CardId = cardId, ShadowOffset = new(-10, 1), ShadowOpacity = 0.4f, GlobalPosition = startPosition, RotationDegrees = startAngle };
				GetTree().CreateTimer(waitTime * i).Timeout += () => container.AddChild(cardDisplay);
			}
		}
		DealToHand(ActionHand, ActionHandSize, ActionDeck, _actionDiscardPile, new Vector2(-200f, 100f), -30f);
		DealToHand(TopicHand, TopicHandSize, TopicDeck, _topicDiscardPile, new Vector2(500f, 100f), 30f);
	}

	// Equivalent of Deal, but deterministic and instant (for display in the editor)
	private void EditorUpdateHands() {
		if (!Engine.IsEditorHint()) return;

		var seededRandom = new Random(0);
		void UpdateHand(HandContainer container, int handSize, Godot.Collections.Dictionary<string, int> deckLayout) {
			if (!container.IsValid() || deckLayout is null) return;
			
			foreach (var child in container.GetChildren()) { container.RemoveChild(child); child.QueueFree(); }

			var tempDeck = new Deck(deckLayout);
			tempDeck.Shuffle(seededRandom);

			foreach (var i in RangeOf<int>.UpTo(handSize)) {
				var cardId = NoRepeatsInHand ? tempDeck.DrawWhere(id => container.FindChildrenWhere<CardDisplay>(card => card.CardId == id).Count == 0) : tempDeck.Draw();
				if (cardId is null) break;
				var cardDisplay = new CardDisplay{ CardId = cardId, ShadowOffset = new(-10, 1), ShadowOpacity = 0.4f };
				container.AddChild(cardDisplay);
			}
		}

		UpdateHand(ActionHand, ActionHandSize, FullLayoutActionDeck);
		UpdateHand(TopicHand, TopicHandSize, FullLayoutTopicDeck);
	}
}
