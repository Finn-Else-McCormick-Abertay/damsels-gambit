using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

public partial class SplashScreen : Control
{
    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseMotion || @event is InputEventScreenDrag) return;
        GameManager.SwitchToMainMenu();
    }
}
