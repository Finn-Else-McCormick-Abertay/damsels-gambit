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
    }

    public override void _ExitTree() {
        GameManager.CardGameChanged -= UpdateToNewController;
        _controller?.TryDisconnect(CardGameController.SignalName.GameStart, new Callable(this, MethodName.Show));
        _controller?.TryDisconnect(CardGameController.SignalName.RoundStart, new Callable(this, MethodName.Hide));
        this.TryDisconnect(BaseButton.SignalName.Pressed, new Callable(_controller, CardGameController.MethodName.ForceSkipIntro));
    }

    private void UpdateToNewController() {
        Hide();
        if (_controller.IsValid()) {
            _controller.TryDisconnect(CardGameController.SignalName.GameStart, new Callable(this, MethodName.Show));
            _controller.TryDisconnect(CardGameController.SignalName.RoundStart, new Callable(this, MethodName.Hide));
            this.TryDisconnect(BaseButton.SignalName.Pressed, new Callable(_controller, CardGameController.MethodName.ForceSkipIntro));
        }
        _controller = GameManager.CardGameController;
        if (_controller.IsValid()) {
            Visible = !_controller.Started;
            _controller.TryConnect(CardGameController.SignalName.GameStart, new Callable(this, MethodName.Show));
            _controller.TryConnect(CardGameController.SignalName.RoundStart, new Callable(this, MethodName.Hide));
            this.TryConnect(BaseButton.SignalName.Pressed, new Callable(_controller, CardGameController.MethodName.ForceSkipIntro));
        }
    }
}
