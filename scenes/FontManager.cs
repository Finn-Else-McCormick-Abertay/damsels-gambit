using Godot;

namespace DamselsGambit;

public static class FontManager
{    
	public static class Fonts
	{
		public static readonly Font ArgosGeorge = ResourceLoader.Load<Font>("res://assets/fonts/argos-george/ArgosGeorge.ttf");
		public static readonly Font Vinque = ResourceLoader.Load<Font>("res://assets/fonts/vinque/Vinque Rg.otf");
		public static readonly Font OpenDyslexic = ResourceLoader.Load<Font>("res://assets/fonts/open-dyslexic/OpenDyslexicEditedForDamsel'sGambit.otf");
	}
	
	private static readonly FontVariation _mainDynamicFont = ResourceLoader.Load<FontVariation>("res://assets/fonts/main_ui_dynamic_font.tres");
	public static Font DefaultFont { get => _mainDynamicFont?.BaseFont; set => _mainDynamicFont.BaseFont = value; }
}
