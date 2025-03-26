using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

public partial class SkipIntroController : Button
{
    private CardGameController _controller;

    public override void _EnterTree() {
        GameManager.CardGameChanged += UpdateToNewController;
        UpdateToNewController();
        this.TryConnect(BaseButton.SignalName.Pressed, OnPressed);
    }

    public override void _ExitTree() {
        GameManager.CardGameChanged -= UpdateToNewController;
        DisconnectFromController();
        this.TryDisconnect(BaseButton.SignalName.Pressed, OnPressed);
    }

    private void DisconnectFromController() {
        Hide();
        if (_controller.IsValid()) {
            _controller.TryDisconnect(CardGameController.SignalName.GameStart, Show);
            _controller.TryDisconnect(CardGameController.SignalName.RoundStart, new Callable(this, MethodName.OnRoundStart));
        }
        _controller = null;
    }

    // Connected to GameManager's CardGameChanged delegate
    private void UpdateToNewController() {
        DisconnectFromController();
        _controller = GameManager.CardGameController;
        if (_controller.IsValid()) {
            Visible = !_controller.Started && _controller.IntroSkippable;
            _controller.TryConnect(CardGameController.SignalName.GameStart, Show);
            _controller.TryConnect(CardGameController.SignalName.RoundStart, new Callable(this, MethodName.OnRoundStart));
        }
    }

    // Connected to CardGameController's RoundStart signal
    public void OnRoundStart(int round) => Hide();

    // Connected to own Pressed signals
    private void OnPressed() {
        _controller?.ForceSkipIntro();
        Hide();
        ReleaseFocus();
    }
}
