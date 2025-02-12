using DamselsGambit;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using System;
using System.Linq;

public partial class CardGameController : Control
{
	[Export] AffectionMeter AffectionMeter { get; set; }
	[Export] HandContainer SubjectHand { get; set; }
	[Export] HandContainer ModifierHand { get; set; }
	[Export] Button PlayButton { get; set; }

	private readonly int _scoreMax = 10, _scoreMin = -10;
	public int Score { get; set { field = value; AffectionMeter.ValuePercent = (Score / (float)(_scoreMax + _scoreMin)) - _scoreMin / (float)(_scoreMax + _scoreMin); } }

	public override void _Ready() {
		PlayButton?.TryConnect(Button.SignalName.Pressed, new Callable(this, MethodName.PlayHand));
	}

	public override void _Process(double delta) {
		PlayButton.Disabled = SubjectHand.GetSelected().Count() != 1 || ModifierHand.GetSelected().Count() != 1;
	}

	private void PlayHand() {
		var selectedSubject = SubjectHand.GetSelected().First();
		var selectedModifier = ModifierHand.GetSelected().First();

		DialogueManager.Run($"{selectedModifier.CardId}+{selectedSubject.CardId}");
	}
}
