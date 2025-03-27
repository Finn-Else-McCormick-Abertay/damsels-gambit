using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;

namespace DamselsGambit.Notebook;

[Tool]
public partial class ProfilePage : Control
{
	[Export] public Node DialogueView { get; set; }

    [Export] public Button PauseButton { get; set { field = value; UpdateShadows(); } }
    [Export] public Button ProfileButton { get; set { field = value; UpdateShadows(); } }

    private readonly List<TextureRect> _shadows = [];
    private readonly Dictionary<TextureRect, Tween> _shadowFadeTweens = [];

    private void UpdateShadows() {
        _shadows.Clear();
        foreach (var button in new List<Button>(){ PauseButton, ProfileButton }) { if (button.IsValid() && button.FindChildOfType<TextureRect>() is TextureRect textureRect) _shadows.Add(textureRect); }
    }

    public void FadeShadows(bool visible, double time) {
        foreach (var shadow in _shadows) {
            // Clean up existing tweens if any
            if (_shadowFadeTweens.TryGetValue(shadow, out var oldTween) && oldTween.IsValid()) oldTween.Kill();
             _shadowFadeTweens.Remove(shadow);
            if (shadow.IsInvalid()) continue;

            if (time <= 0 || shadow.Visible == visible) { shadow.Visible = visible; shadow.Modulate = shadow.Modulate with { A = 1f }; }
            else {
                shadow.Modulate = shadow.Modulate with { A = shadow.Visible ? 1f : 0f };
                shadow.Visible = true;
                var tween = CreateTween();
                tween.TweenProperty(shadow, CanvasItem.PropertyName.Modulate.ToString(), shadow.Modulate with { A = visible ? 1f : 0f }, time);
                tween.TweenProperty(shadow, CanvasItem.PropertyName.Visible.ToString(), visible, 0);
            }
        }
    }
    
    public void SetShadows(bool visible) => FadeShadows(visible, 0.0);

    /*[Export] public Control MouseIndicator { get; set; }

    public override void _GuiInput(InputEvent @event) {
        Console.Info("Profile page gui input: ", @event);
        if (@event is InputEventMouse mouseEvent) {
            if (MouseIndicator is not null) {
                MouseIndicator.Position = mouseEvent.Position;
            }
        }
    }*/
}
