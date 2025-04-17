using DamselsGambit.Util;
using Godot;
using System;
using System.Linq;

namespace DamselsGambit;

public partial class SplashScreen : Control
{
    [Export] public bool UseSplashScreen = true;
    [Export] public double BootHoldTime = 0.05;
    [Export] public double BootFadeTime = 1.5;
    [Export] public double SplashHoldTime = 0;

    [Export] public string IntroAnimation { get; set; } = "intro";
    [Export] public string IdleAnimation { get; set; } = "idle";

    private Tween _bootFadeTween;
    private Timer _splashHoldTimer;
    private AnimationPlayer _animationPlayer;

    public override void _Ready() {
        if (!UseSplashScreen) { MouseFilter = MouseFilterEnum.Ignore; foreach (var child in GetChildren().Where(x => x is CanvasItem).Cast<CanvasItem>()) child.Hide(); }

        if (UseSplashScreen) {
            _splashHoldTimer = new Timer() { WaitTime = BootHoldTime + SplashHoldTime, Autostart = true, OneShot = true };
            AddChild(_splashHoldTimer);
        }

        _animationPlayer = this.FindChildOfType<AnimationPlayer>();

        var colorRect = new ColorRect() { Color = ProjectSettings.GetSettingWithOverride("application/boot_splash/bg_color").AsColor(), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(colorRect);
        colorRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _bootFadeTween = CreateTween();
        _bootFadeTween.TweenInterval(BootHoldTime);
        if (UseSplashScreen) _bootFadeTween.TweenCallback(AnimateSplashStart);
        _bootFadeTween.TweenProperty(colorRect, "color:a", 0f, BootFadeTime).SetTrans(Tween.TransitionType.Quad);

        if (ProjectSettings.GetSettingWithOverride("application/boot_splash/show_image").AsBool()) {
            var bootSplashImage = ResourceLoader.Load<Texture2D>(ProjectSettings.GetSettingWithOverride("application/boot_splash/image").AsString());
            var textureRect = new TextureRect() { Texture = bootSplashImage, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(textureRect);
            textureRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            _bootFadeTween.Parallel();
            _bootFadeTween.TweenProperty(textureRect, "modulate:a", 0f, BootFadeTime);
        }

        if (!UseSplashScreen) _bootFadeTween.TweenCallback(GameManager.SwitchToMainMenu);
    }

    public override void _Input(InputEvent @event) {
        if ((_bootFadeTween.IsValid() && _bootFadeTween.IsRunning()) || (UseSplashScreen && !_splashHoldTimer.IsStopped())) return;
        if (@event is InputEventMouseMotion || @event is InputEventScreenDrag) return;
        GameManager.SwitchToMainMenu();
    }

    private void AnimateSplashStart() {
        if (!_animationPlayer.IsValid()) return;

        void PlayIdleAnimation() { if (!_animationPlayer.HasAnimation(IdleAnimation)) return; _animationPlayer.Play(IdleAnimation); }

        if (_animationPlayer.HasAnimation(IntroAnimation)) {
            _animationPlayer.Play(IntroAnimation);
            void IntroAnimFinished(StringName _) {
                _animationPlayer.AnimationFinished -= IntroAnimFinished;
                PlayIdleAnimation();
            }
            _animationPlayer.AnimationFinished += IntroAnimFinished;
        }
        else PlayIdleAnimation();
    }
}
