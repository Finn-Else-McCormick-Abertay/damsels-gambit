using DamselsGambit.Util;
using Godot;
using System;
using System.Linq;

namespace DamselsGambit;

public partial class SplashScreen : Control
{
    [Export] public bool UseSpashScreen = false;
    [Export] public double BootFadeTime = 1.5;

    private Tween _bootFadeTween;

    public override void _Ready() {
        if (!UseSpashScreen) { MouseFilter = MouseFilterEnum.Ignore; foreach (var child in GetChildren().Where(x => x is CanvasItem).Cast<CanvasItem>()) child.Hide(); }

        var colorRect = new ColorRect() { Color = ProjectSettings.GetSettingWithOverride("application/boot_splash/bg_color").AsColor(), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(colorRect);
        colorRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _bootFadeTween = CreateTween();
        _bootFadeTween.TweenInterval(0.05);
        _bootFadeTween.TweenProperty(colorRect, "color:a", 0f, BootFadeTime).SetTrans(Tween.TransitionType.Quad);

        if (ProjectSettings.GetSettingWithOverride("application/boot_splash/show_image").AsBool()) {
            var bootSplashImage = ResourceLoader.Load<Texture2D>(ProjectSettings.GetSettingWithOverride("application/boot_splash/image").AsString());
            var textureRect = new TextureRect() { Texture = bootSplashImage, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(textureRect);
            textureRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            _bootFadeTween.Parallel();
            _bootFadeTween.TweenProperty(textureRect, "modulate:a", 0f, BootFadeTime);
        }

        if (!UseSpashScreen) _bootFadeTween.TweenCallback(GameManager.SwitchToMainMenu);
    }

    public override void _Input(InputEvent @event) {
        if (_bootFadeTween.IsValid() && _bootFadeTween.IsRunning()) return;
        if (@event is InputEventMouseMotion || @event is InputEventScreenDrag) return;
        GameManager.SwitchToMainMenu();
    }
}
