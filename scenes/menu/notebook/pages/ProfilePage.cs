using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DamselsGambit.Notebook;

[Tool]
public partial class ProfilePage : Control
{
	[Export] public Node DialogueView { get; set; }

	[Export] public Button PauseButton { get; set { field = value; UpdateShadows(); } }
	[Export] public Button ProfileButton { get; set { field = value; UpdateShadows(); } }

	[Export] private Godot.Collections.Dictionary<Node, double> ShadowFadedAlpha { get; set; } = [];

	private readonly Dictionary<Node, TextureRect> _shadows = [];
	private readonly Dictionary<TextureRect, Tween> _shadowFadeTweens = [];
	private readonly Dictionary<TextureRect, bool> _shadowLogicallyVisible = [];

	private void UpdateShadows() {
		_shadows.Clear();
		foreach (var button in new List<Button>(){ PauseButton, ProfileButton }) { if (button.IsValid() && button.FindChildOfType<TextureRect>() is TextureRect textureRect) _shadows.Add(button, textureRect); }
		foreach (var invalidShadow in _shadowLogicallyVisible.Select(x => x.Key).Where(shadow => !_shadows.Any(x => shadow == x.Value))) _shadowLogicallyVisible.Remove(invalidShadow);
		foreach (var (node, shadow) in _shadows) { if (!_shadowLogicallyVisible.ContainsKey(shadow)) _shadowLogicallyVisible.Add(shadow, (shadow.Visible && shadow.Modulate.A > 0)); }
	}

	public void FadeShadows(bool visible, double time) {
		foreach (var (button, shadow) in _shadows) {
			// Clean up existing tweens if any
			if (_shadowFadeTweens.TryGetValue(shadow, out var oldTween) && oldTween.IsValid()) oldTween.Kill();
			 _shadowFadeTweens.Remove(shadow);
			if (shadow.IsInvalid()) continue;

			if (time <= 0 || _shadowLogicallyVisible[shadow] == visible) { shadow.Visible = visible; shadow.Modulate = shadow.Modulate with { A = 1f }; }
			else {
				float finalAlpha = (float)(ShadowFadedAlpha.TryGetValue(button, out double fadeAlpha) ? fadeAlpha : 0);
				//Console.Info($"Fade shadow of {button.ToPrettyString()} to {visible} : {1f} -> {finalAlpha}", true);
				shadow.Modulate = shadow.Modulate with { A = shadow.Visible ? 1f : finalAlpha };
				shadow.Visible = true;
				var tween = CreateTween();
				tween.TweenProperty(shadow, CanvasItem.PropertyName.Modulate.ToString(), shadow.Modulate with { A = visible ? 1f : finalAlpha }, time);
				tween.TweenCallback(() => { _shadowLogicallyVisible[shadow] = visible; if (!visible && finalAlpha == 0f) shadow.Hide(); });
			}
		}
	}
	
	public void SetShadows(bool visible) => FadeShadows(visible, 0.0);
}
