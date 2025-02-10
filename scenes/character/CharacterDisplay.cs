using Godot;
using System;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/character_display.svg")]
public partial class CharacterDisplay : Sprite2D
{
	[Export] public string CharacterName { get; set { field = value; TextureRoot = $"res://assets/characters/{CharacterName}/"; } }	
	[Export] public string SpriteName { get; set { field = value; TexturePath = TextureRoot + SpriteName + ".png"; } }

	private string TextureRoot { get; set { field = value; TexturePath = TextureRoot + SpriteName + ".png"; } }
	private string TexturePath { get; set { field = value; Texture = ResourceLoader.Exists(TexturePath) ? ResourceLoader.Load<Texture2D>(TexturePath) : null; } }
}
