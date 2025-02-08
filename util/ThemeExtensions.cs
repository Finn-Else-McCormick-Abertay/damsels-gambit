using Godot;

namespace DamselsGambit.Util;

#nullable enable

public static class ThemeExtensions
{
    public static Color? TryGetColor(this Theme self, StringName property, StringName themeType) => self.HasColor(property, themeType) ? self.GetColor(property, themeType) : null;
    public static int? TryGetConstant(this Theme self, StringName property, StringName themeType) => self.HasConstant(property, themeType) ? self.GetConstant(property, themeType) : null;
    public static Font? TryGetFont(this Theme self, StringName property, StringName themeType) => self.HasFont(property, themeType) ? self.GetFont(property, themeType) : null;
    public static int? TryGetFontSize(this Theme self, StringName property, StringName themeType) => self.HasFontSize(property, themeType) ? self.GetFontSize(property, themeType) : null;
    public static StyleBox? TryGetStylebox(this Theme self, StringName property, StringName themeType) => self.HasStylebox(property, themeType) ? self.GetStylebox(property, themeType) : null;
    public static Texture2D? TryGetIcon(this Theme self, StringName property, StringName themeType) => self.HasIcon(property, themeType) ? self.GetIcon(property, themeType) : null;
}