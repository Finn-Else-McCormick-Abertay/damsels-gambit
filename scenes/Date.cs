using Godot;
using System;
using DamselsGambit.Util;

namespace DamselsGambit;

public abstract partial class Date : Control
{
    protected CardGameController _cardGameController;

    public override void _Ready() {
        _cardGameController = this.FindChildOfType<CardGameController>();
        _cardGameController.Connect(CardGameController.SignalName.GameEnd, Callable.From(OnGameEnd));
    }

    public abstract void OnGameEnd();
}
