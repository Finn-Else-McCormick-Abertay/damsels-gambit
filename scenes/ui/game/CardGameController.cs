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

	[Export] public string SuitorName { get; set { field = value; _suitorId = Case.ToSnake(SuitorName); SuitorProfile?.OnReady(x => x.SuitorName = SuitorName); } }
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
	[Export] public Godot.Collections.Dictionary<string, int> TopicDeck { get; set; }
	[Export] public Godot.Collections.Dictionary<string, int> ActionDeck { get; set; }

	private List<string> _topicDeckWorking, _actionDeckWorking;

	public ReadOnlyCollection<string> RemainingTopicDeck => _topicDeckWorking?.AsReadOnly();
	public ReadOnlyCollection<string> RemainingActionDeck => _actionDeckWorking?.AsReadOnly();

	[ExportGroup("Nodes")]
	[Export] public AffectionMeter AffectionMeter { get; set; }
	[Export] public RoundMeter RoundMeter { get; set; }
	[Export] public HandContainer TopicHand { get; set; }
	[Export] public HandContainer ActionHand { get; set; }
	[Export] public Button PlayButton { get; set; }
	[Export] public SuitorProfile SuitorProfile { get; set; }

	public override void _Ready() {
		if (Engine.IsEditorHint()) return;

		PlayButton?.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.PlayHand));
		PlayButton.Disabled = true;

		foreach (var child in TopicHand.GetChildren()) { TopicHand.RemoveChild(child); child.QueueFree(); }
		foreach (var child in ActionHand.GetChildren()) { ActionHand.RemoveChild(child); child.QueueFree(); }
		
		Hide(); Round = 1;
	}

	public void BeginGame() {
		static List<string> CreateWorkingDeck(Godot.Collections.Dictionary<string, int> deck) {
			List<string> working = [];
			foreach (var (card, count) in deck) { for (int i = 0; i < count; ++i) { working.Add(card); } }
			return working;
		}

		// Create and shuffle deck
		_topicDeckWorking = [..CreateWorkingDeck(TopicDeck).OrderBy(x => Random.Shared.Next())];
		_actionDeckWorking = [..CreateWorkingDeck(ActionDeck).OrderBy(x => Random.Shared.Next())];
		
		EmitSignal(SignalName.GameStart);
		
		DialogueManager.TryRun($"{_suitorId}/intro").AndThen(() => {
			Show(); Deal();
			EmitSignal(SignalName.RoundStart);
		});
	}

	public override void _Process(double delta) {
		if (Engine.IsEditorHint()) return;

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
		if (Engine.IsEditorHint()) return;

		CardDisplay selectedTopic = TopicHand.GetSelected().First(), selectedAction = ActionHand.GetSelected().First();

		var dialogueNode = $"{_suitorId}/{selectedAction.CardId.ToString().StripFront("action/")}+{selectedTopic.CardId.ToString().StripFront("topic/")}";

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

		AffectionMeter.Hide(); RoundMeter.Hide(); TopicHand.Hide(); ActionHand.Hide(); PlayButton.Hide(); SuitorProfile.Hide();

		DialogueManager
			.TryRun($"{_suitorId}/ending/{Score switch { _ when Score >= LoveThreshold => "love", _ when Score <= HateThreshold => "hate", _ => "neutral" }}")
			.AndThen(() => EmitSignal(SignalName.GameEnd));
	}

	/*private void OnGameEndDialogueComplete() {
		if (Engine.IsEditorHint()) return;

		//DialogueManager.Runner.TryDisconnect(DialogueRunner.SignalName.onDialogueComplete, new Callable(this, MethodName.OnGameEndDialogueComplete));
		var endScreen = ResourceLoader.Load<PackedScene>("res://scenes/ui/end_screen.tscn").Instantiate<EndScreen>();
		AddChild(endScreen);
		endScreen.MessageLabel.Text = Score switch {
			_ when Score >= LoveThreshold => "Prepare for marriage.",
			_ when Score <= HateThreshold => "Prepare for war.",
			_ => "You win!"
		};
	}*/

	private void Deal() {
		if (Engine.IsEditorHint()) return;

		void DealToHand(HandContainer container, List<string> workingDeck, Vector2 startPosition, float startAngle, double waitTime = 0.2) {
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
		DealToHand(ActionHand, _actionDeckWorking, new Vector2(-200f, 100f), -30f);
		DealToHand(TopicHand, _topicDeckWorking, new Vector2(500f, 100f), 30f);
	}
}
