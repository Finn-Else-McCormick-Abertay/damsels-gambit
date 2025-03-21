using Godot;

namespace DamselsGambit;

public partial class FrostholmDate : BaseDate
{
	public override void OnGameEnd() {
		var endScreen = ResourceLoader.Load<PackedScene>("res://scenes/menu/end/end_screen.tscn").Instantiate<EndScreen>();
		AddChild(endScreen);
		endScreen.MessageLabel.Text = _cardGameController.Score switch {
			_ when _cardGameController.Score >= _cardGameController.LoveThreshold => "Prepare for marriage.",
			_ when _cardGameController.Score <= _cardGameController.HateThreshold => "Prepare for war.",
			_ => "You win!"
		};
	}
}
