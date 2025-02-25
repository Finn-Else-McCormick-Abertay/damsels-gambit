using System;
using System.Linq;
using Godot;
using Godot.Collections;
using DamselsGambit.Util;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/affection_meter.svg")]
public partial class AffectionMeter : Control, IReloadableToolScript
{
	[Export(PropertyHint.Range, "0,1")] public float ValuePercent { get; set { field = value; QueueRedraw(); } } = 0.5f;
	[Export(PropertyHint.Range, "0,1")] public float LovePercent { get; set { field = value; QueueRedraw(); } } = 0.3f;
	[Export(PropertyHint.Range, "0,1")] public float HatePercent { get; set { field = value; QueueRedraw(); } } = 0.3f;
	
	static readonly StringName TypeName = nameof(AffectionMeter);
	public static class ThemeProperties
	{
		public static class Stylebox {
			public static readonly StringName Under = "under";
			public static readonly StringName Over = "over";
			public static readonly StringName LoveArea = "love_area";
			public static readonly StringName HateArea = "hate_area";
			public static readonly StringName LoveBoundary = "love_boundary";
			public static readonly StringName HateBoundary = "hate_boundary";
			public static readonly StringName MiddleBoundary = "middle_boundary";
			public static readonly StringName ValueBoundary = "value_boundary";
		}
		public static class Icon {
			public static readonly StringName Marker = "marker";
			public static readonly StringName Love = "love";
			public static readonly StringName Hate = "hate";
		}
		public static class Constant {
			public static readonly StringName LoveIconSize = "love_icon_size";
			public static readonly StringName HateIconSize = "hate_icon_size";
			public static readonly StringName MarkerIconSize = "marker_icon_size";
		}
	}

	public override void _Draw() {
		Theme theme = Theme;
		Control node = this;
		while (theme is null && node is not null) {
			node = node.GetParent() as Control;
			theme = node.Theme;
		}
		theme ??= ThemeDB.GetDefaultTheme();

		var under = theme?.TryGetStylebox(ThemeProperties.Stylebox.Under, TypeName) ?? ThemeDB.FallbackStylebox;
		var over = theme?.TryGetStylebox(ThemeProperties.Stylebox.Over, TypeName) ?? new StyleBoxEmpty();
		var love = theme?.TryGetStylebox(ThemeProperties.Stylebox.LoveArea, TypeName) ?? new StyleBoxFlat{ BgColor = Colors.Red };
		var hate = theme?.TryGetStylebox(ThemeProperties.Stylebox.HateArea, TypeName) ?? new StyleBoxFlat{ BgColor = Colors.Black };

		var loveBound = theme?.TryGetStylebox(ThemeProperties.Stylebox.LoveBoundary, TypeName) ?? new StyleBoxEmpty();
		var hateBound = theme?.TryGetStylebox(ThemeProperties.Stylebox.HateBoundary, TypeName) ?? new StyleBoxEmpty();
		var middleBound = theme?.TryGetStylebox(ThemeProperties.Stylebox.MiddleBoundary, TypeName) ?? new StyleBoxEmpty();
		var valueBound = theme?.TryGetStylebox(ThemeProperties.Stylebox.ValueBoundary, TypeName) ?? new StyleBoxLine();

		var marker = theme?.TryGetIcon(ThemeProperties.Icon.Marker, TypeName);
		var markerScale = theme?.TryGetConstant(ThemeProperties.Constant.MarkerIconSize, TypeName) ?? marker?.GetHeight() ?? 0; if (markerScale < 0) { markerScale = marker?.GetHeight() ?? 0; }
		Vector2 markerSize = marker is not null ? marker.GetSize() / marker.GetHeight() * markerScale : new Vector2();

		var loveIcon = theme?.TryGetIcon(ThemeProperties.Icon.Love, TypeName);
		var loveScale = theme?.TryGetConstant(ThemeProperties.Constant.LoveIconSize, TypeName) ?? loveIcon?.GetHeight() ?? 0; if (loveScale < 0) { loveScale = loveIcon?.GetHeight() ?? 0; }
		Vector2 loveSize = loveIcon is not null ? loveIcon.GetSize() / loveIcon.GetHeight() * loveScale : new();

		var hateIcon = theme?.TryGetIcon(ThemeProperties.Icon.Hate, TypeName);
		var hateScale = theme?.TryGetConstant(ThemeProperties.Constant.HateIconSize, TypeName) ?? hateIcon?.GetHeight() ?? 0; if (hateScale < 0) { hateScale = hateIcon?.GetHeight() ?? 0; }
		Vector2 hateSize = hateIcon is not null ? hateIcon.GetSize() / hateIcon.GetHeight() * hateScale : new();

		DrawStyleBox(love, new Rect2(0f, 0f, Size with { Y = Size.Y * LovePercent }));
		DrawStyleBox(hate, new Rect2(0f, (1 - HatePercent) * Size.Y, Size with { Y = Size.Y * HatePercent }));
		
		DrawStyleBox(under, new Rect2(0f, 0f, Size));
		
		DrawStyleBox(loveBound, new Rect2(0f, Size.Y * LovePercent, Size.X, 0f));
		DrawStyleBox(hateBound, new Rect2(0f, Size.Y * (1 - HatePercent), Size.X, 0f));
		DrawStyleBox(middleBound, new Rect2(0f, Size.Y * 0.5f, Size.X, 0f));

		DrawStyleBox(valueBound, new Rect2(0f, Size.Y * ValuePercent, Size.X, 0f));
		
		DrawStyleBox(over, new Rect2(0f, 0f, Size));


		if (marker is not null)	  DrawTextureRect(marker, new(Size.X / 2f - markerSize.X / 2f, Size.Y * ValuePercent - markerSize.Y / 2f, markerSize), false);
		if (loveIcon is not null) DrawTextureRect(loveIcon, new(Size.X / 2f - loveSize.X / 2f, 0f - loveSize.Y / 2f, loveSize), false);
		if (hateIcon is not null) DrawTextureRect(hateIcon, new(Size.X / 2f - hateSize.X / 2f, Size.Y - hateSize.Y / 2f, hateSize), false);
	}
}
