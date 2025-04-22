using Godot;

namespace DamselsGambit.Util;

#nullable enable

public static class ThemeExtensions
{
    public static bool TryGetColor(this Theme self, StringName property, StringName themeType, out Color color) { color = self.GetColor(property, themeType); return self.HasColor(property, themeType); }
    public static bool TryGetConstant(this Theme self, StringName property, StringName themeType, out int constant) { constant = self.GetConstant(property, themeType); return self.HasConstant(property, themeType); }
    public static bool TryGetFont(this Theme self, StringName property, StringName themeType, out Font font) { font = self.GetFont(property, themeType); return self.HasFont(property, themeType); }
    public static bool TryGetFontSize(this Theme self, StringName property, StringName themeType, out int fontSize) { fontSize = self.GetFontSize(property, themeType); return self.HasFontSize(property, themeType); }
    public static bool TryGetStylebox(this Theme self, StringName property, StringName themeType, out StyleBox stylebox) { stylebox = self.GetStylebox(property, themeType); return self.HasStylebox(property, themeType); }
    public static bool TryGetIcon(this Theme self, StringName property, StringName themeType, out Texture2D icon) { icon = self.GetIcon(property, themeType); return self.HasIcon(property, themeType); }

    public static Color GetColorOr(this Theme self, StringName property, StringName themeType, Color fallback) => self.HasColor(property, themeType) ? self.GetColor(property, themeType) : fallback;
    public static int GetConstantOr(this Theme self, StringName property, StringName themeType, int fallback) => self.HasConstant(property, themeType) ? self.GetConstant(property, themeType) : fallback;
    public static Font GetFontOr(this Theme self, StringName property, StringName themeType, Font fallback) => self.HasFont(property, themeType) ? self.GetFont(property, themeType) : fallback;
    public static int GetFontSizeOr(this Theme self, StringName property, StringName themeType, int fallback) => self.HasFontSize(property, themeType) ? self.GetFontSize(property, themeType) : fallback;
    public static StyleBox GetStyleboxOr(this Theme self, StringName property, StringName themeType, StyleBox fallback) => self.HasStylebox(property, themeType) ? self.GetStylebox(property, themeType) : fallback;
    public static Texture2D GetIconOr(this Theme self, StringName property, StringName themeType, Texture2D fallback) => self.HasIcon(property, themeType) ? self.GetIcon(property, themeType) : fallback;

    public static Color? GetColorOrDefault(this Theme self, StringName property, StringName themeType) => self.HasColor(property, themeType) ? self.GetColor(property, themeType) : default;
    public static int? GetConstantOrDefault(this Theme self, StringName property, StringName themeType) => self.HasConstant(property, themeType) ? self.GetConstant(property, themeType) : default;
    public static Font? GetFontOrDefault(this Theme self, StringName property, StringName themeType) => self.HasFont(property, themeType) ? self.GetFont(property, themeType) : default;
    public static int? GetFontSizeOrDefault(this Theme self, StringName property, StringName themeType) => self.HasFontSize(property, themeType) ? self.GetFontSize(property, themeType) : default;
    public static StyleBox? GetStyleboxOrDefault(this Theme self, StringName property, StringName themeType) => self.HasStylebox(property, themeType) ? self.GetStylebox(property, themeType) : default;
    public static Texture2D? GetIconOrDefault(this Theme self, StringName property, StringName themeType) => self.HasIcon(property, themeType) ? self.GetIcon(property, themeType) : default;
}