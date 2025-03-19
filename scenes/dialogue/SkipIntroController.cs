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
        this.TryConnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnPressed));
    }

    public override void _ExitTree() {
        GameManager.CardGameChanged -= UpdateToNewController;
        DisconnectFromController();
        this.TryDisconnect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnPressed));
    }

    private void DisconnectFromController() {
        Hide();
        if (_controller.IsValid()) {
            _controller.TryDisconnect(CardGameController.SignalName.GameStart, new Callable(this, CanvasItem.MethodName.Show));
            _controller.TryDisconnect(CardGameController.SignalName.RoundStart, new Callable(this, MethodName.OnRoundStart));
        }
        _controller = null;
    }

    private void UpdateToNewController() {
        DisconnectFromController();
        _controller = GameManager.CardGameController;
        if (_controller.IsValid()) {
            Visible = !_controller.Started && _controller.IntroSkippable;
            _controller.TryConnect(CardGameController.SignalName.GameStart, new Callable(this, CanvasItem.MethodName.Show));
            _controller.TryConnect(CardGameController.SignalName.RoundStart, new Callable(this, MethodName.OnRoundStart));
        }
    }

    public void OnRoundStart(int round) => Hide();

    private void OnPressed() {
        _controller?.ForceSkipIntro();
        Hide();
        ReleaseFocus();
    }
}
