using System.Collections.Generic;
using Godot;

namespace DamselsGambit;

public static class FontManager
{
	public enum FontState { Default, OpenDyslexic }

	public static FontState Font { get; set { field = value; UpdateDynamicFonts(); } }
	
	private static readonly Font Vinque = ResourceLoader.Load<Font>("res://assets/fonts/vinque/Vinque Rg.otf"),
		Matura = ResourceLoader.Load<Font>("res://assets/fonts/maturasc/MATURASC.ttf"),
		OpenDyslexic = ResourceLoader.Load<Font>("res://assets/fonts/open-dyslexic/OpenDyslexic3-Regular.ttf");

	private static readonly FontVariation _mainDynamicFont = ResourceLoader.Load<FontVariation>("res://assets/fonts/main_ui_dynamic_font.tres"),
		_headerDynamicFont = ResourceLoader.Load<FontVariation>("res://assets/fonts/main_ui_dynamic_font.tres"),
		_cardTypeDynamicFont = ResourceLoader.Load<FontVariation>("res://assets/fonts/card_type_dynamic_font.tres");

	private static void UpdateDynamicFonts() {
		_mainDynamicFont.BaseFont = Font switch { FontState.OpenDyslexic => OpenDyslexic, _ => Vinque };
		_headerDynamicFont.BaseFont = Font switch { FontState.OpenDyslexic => OpenDyslexic, _ => Vinque };
		_cardTypeDynamicFont.BaseFont = Font switch { FontState.OpenDyslexic => OpenDyslexic, _ => Matura };
	}
}