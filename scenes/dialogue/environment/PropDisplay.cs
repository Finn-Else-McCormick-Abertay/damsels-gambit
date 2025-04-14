using Godot;
using System;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using System.Collections.Generic;

namespace DamselsGambit.Environment;

[Tool, GlobalClass]
public partial class PropDisplay : Sprite2D, IEnvironmentDisplay
{
	[Export] public StringName PropName { get => !string.IsNullOrEmpty(field) ? field : Name; set { field = value; UpdateTexture(); } } = "";
	[Export] public int Variant { get; set { field = value; UpdateTexture(); } } = 0;

	private string TexturePath { get; set { field = value; Texture = ResourceLoader.Exists(TexturePath) ? ResourceLoader.Load<Texture2D>(TexturePath) : new PlaceholderTexture2D() { Size = new(200,200) }; } }

	private void UpdateTexture() => TexturePath = $"res://assets/items/{Case.ToSnake(PropName)}{(Variant > 0 ? $"_{Variant}" : "")}.png";

	public static IEnumerable<int> GetValidVariants(string propName) {
		List<int> validVariants = [];
		if (ResourceLoader.Exists($"res://assets/items/{Case.ToSnake(propName)}.png")) validVariants.Add(0);
		int variant = 1; while (ResourceLoader.Exists($"res://assets/items/{Case.ToSnake(propName)}_{variant}.png")) { validVariants.Add(variant); variant++; }
		return validVariants;
	}
	
	private bool _initiallyVisible;

	public override void _EnterTree() { if(!Engine.IsEditorHint()) { EnvironmentManager.Register(this); _initiallyVisible = Visible; Hide(); } }
	public override void _ExitTree() { if(!Engine.IsEditorHint()) EnvironmentManager.Deregister(this); }
	
	public void RestoreInitialVisibility() => Visible = _initiallyVisible;
}
