using Godot;

namespace DamselsGambit;

public partial class TutorialDate : BaseDate
{
    public override void OnGameEnd() => GameManager.SwitchToCardGameScene("res://scenes/dates/frostholm_date.tscn");
}
