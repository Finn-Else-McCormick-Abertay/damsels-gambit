using Godot;

namespace DamselsGambit;

public partial class TutorialDate : Date
{
    public override void OnGameEnd() => GameManager.SwitchToCardGameScene("res://scenes/frostholm_date.tscn");
}
