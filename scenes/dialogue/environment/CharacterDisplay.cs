using Godot;
using System;

namespace DamselsGambit.Environment;

[Tool, GlobalClass, Icon("res://assets/editor/icons/character_display.svg")]
public partial class CharacterDisplay : Sprite2D, IEnvironmentDisplay
{
	[Export] public StringName CharacterName { get; set { field = value; TextureRoot = $"res://assets/characters/{CharacterName}/"; } }	
	[Export] public string SpriteName { get; set { field = value; TexturePath = TextureRoot + SpriteName + ".png"; } }

	public bool SpriteExists => Texture != null;

	private string TextureRoot { get; set { field = value; TexturePath = TextureRoot + SpriteName + ".png"; } }
	private string TexturePath { get; set { field = value; Texture = ResourceLoader.Exists(TexturePath) ? ResourceLoader.Load<Texture2D>(TexturePath) : null; } }

	private bool _initiallyVisible;

    public override void _EnterTree() { if(!Engine.IsEditorHint()) { EnvironmentManager.Register(this); _initiallyVisible = Visible; Hide(); } }
	public override void _ExitTree() { if(!Engine.IsEditorHint()) EnvironmentManager.Deregister(this); }

	public void RestoreInitialVisibility() => Visible = _initiallyVisible;
}
