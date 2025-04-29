using System;
using System.Collections.Generic;
using System.Linq;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public partial class FontManager : Node
{
	public static FontManager Instance { get; private set; }
	public override void _EnterTree() { if (Instance is not null) throw AutoloadException.For(this); Instance = this; }

	public enum FontState { Default, OpenDyslexic }

	private static FontState _fontInternal;
	public static FontState Font { get => _fontInternal; set { _fontInternal = value; UpdateDynamicFonts(); SettingsManager.SetConfig("accessibility", "font", Enum.GetName(Font)); } }
	
	private static readonly Dictionary<string, FontVariation> _dynamicFonts = [];
	private static FontVariation GetDynamicFont(string id) {
		id = Case.ToSnake(id).Trim();
		if (_dynamicFonts.TryGetValue(id, out var fontVariation)) return fontVariation;
		if ($"res://assets/fonts/{id}_dynamic_font.tres" is string path && ResourceLoader.Exists(path)) return _dynamicFonts.GetOrAdd(id, ResourceLoader.Load<FontVariation>(path));
		return null;
	}

	private static void SetDynamicFont(string id, Font font) {
		if (GetDynamicFont(id) is not FontVariation dynamicFont) { Console.Error($"No such dynamic font '{id}', cannot set to {font.ToPrettyString()}"); return; }

		if (font is FontVariation fontVariation)
			foreach (var prop in typeof(FontVariation).GetProperties().Where(prop => typeof(FontVariation).BaseType.GetProperty(prop.Name) is null)) prop.SetValue(dynamicFont, prop.GetValue(fontVariation));
		else {
			dynamicFont.BaseFont = font;
			dynamicFont.VariationFaceIndex = 0; dynamicFont.VariationEmbolden = 0f; dynamicFont.VariationTransform = Transform2D.Identity;
			dynamicFont.SpacingGlyph = 0; dynamicFont.SpacingSpace = 0; dynamicFont.SpacingTop = 0; dynamicFont.SpacingBottom = 0; dynamicFont.BaselineOffset = 0f;
			dynamicFont.VariationOpentype = []; dynamicFont.OpentypeFeatures = []; dynamicFont.Fallbacks = [];
		}
	}
	
	private static readonly Font Vinque = ResourceLoader.Load<Font>("res://assets/fonts/vinque/Vinque Rg.otf");
	private static readonly Font Matura = ResourceLoader.Load<Font>("res://assets/fonts/maturasc/MATURASC.ttf");
	private static readonly FontVariation OpenDyslexic = ResourceLoader.Load<FontVariation>("res://assets/fonts/open-dyslexic/open_dyslexic_normalised.tres");

	private static void UpdateDynamicFonts() {
		SetDynamicFont("main", Font switch { FontState.OpenDyslexic => OpenDyslexic, _ => Vinque });
		SetDynamicFont("header", Font switch { FontState.OpenDyslexic => OpenDyslexic, _ => Vinque });
		SetDynamicFont("card_type", Font switch { FontState.OpenDyslexic => OpenDyslexic, _ => Matura });
	}

	static FontManager() {
		SettingsManager.SetConfigHandler("accessibility", "font", (string fontName) => {
            if (Enum.TryParse(fontName, out FontState result)) {
                _fontInternal = result; UpdateDynamicFonts();
                Console.Info($"accessibility/font set to {fontName}");
            }
            else Console.Warning($"accessibility/font is set to nonexistent font '{fontName}'");
        });
	}
}