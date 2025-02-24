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
	}

	public override void _Draw() {
		var under = Theme?.TryGetStylebox(ThemeProperties.Stylebox.Under, TypeName) ?? ThemeDB.FallbackStylebox;
		var over = Theme?.TryGetStylebox(ThemeProperties.Stylebox.Over, TypeName) ?? new StyleBoxEmpty();
		var love = Theme?.TryGetStylebox(ThemeProperties.Stylebox.LoveArea, TypeName) ?? new StyleBoxFlat{ BgColor = Colors.Red };
		var hate = Theme?.TryGetStylebox(ThemeProperties.Stylebox.HateArea, TypeName) ?? new StyleBoxFlat{ BgColor = Colors.Black };

		var loveBound = Theme?.TryGetStylebox(ThemeProperties.Stylebox.LoveBoundary, TypeName) ?? new StyleBoxEmpty();
		var hateBound = Theme?.TryGetStylebox(ThemeProperties.Stylebox.HateBoundary, TypeName) ?? new StyleBoxEmpty();
		var middleBound = Theme?.TryGetStylebox(ThemeProperties.Stylebox.MiddleBoundary, TypeName) ?? new StyleBoxEmpty();
		var valueBound = Theme?.TryGetStylebox(ThemeProperties.Stylebox.ValueBoundary, TypeName) ?? new StyleBoxLine();

		var marker = Theme?.TryGetIcon(ThemeProperties.Icon.Marker, TypeName);
		var loveIcon = Theme?.TryGetIcon(ThemeProperties.Icon.Love, TypeName);
		var hateIcon = Theme?.TryGetIcon(ThemeProperties.Icon.Hate, TypeName);

		DrawStyleBox(under, new Rect2(0f, 0f, Size));

		DrawStyleBox(love, new Rect2(0f, 0f, Size with { Y = Size.Y * LovePercent }));
		DrawStyleBox(hate, new Rect2(0f, (1 - HatePercent) * Size.Y, Size with { Y = Size.Y * HatePercent }));
		
		DrawStyleBox(loveBound, new Rect2(0f, Size.Y * LovePercent, Size.X, 0f));
		DrawStyleBox(hateBound, new Rect2(0f, Size.Y * (1 - HatePercent), Size.X, 0f));
		DrawStyleBox(middleBound, new Rect2(0f, Size.Y * 0.5f, Size.X, 0f));

		DrawStyleBox(valueBound, new Rect2(0f, Size.Y * ValuePercent, Size.X, 0f));
		if (marker is not null)	  DrawTexture(marker, new(Size.X / 2f - marker.GetWidth() / 2f, Size.Y * ValuePercent - marker.GetHeight() / 2f));
		if (loveIcon is not null) DrawTexture(loveIcon, new(Size.X / 2f - loveIcon.GetWidth() / 2f, 0f - loveIcon.GetHeight() / 2f));
		if (hateIcon is not null) DrawTexture(hateIcon, new(Size.X / 2f - hateIcon.GetWidth() / 2f, Size.Y - hateIcon.GetHeight() / 2f));

		DrawStyleBox(over, new Rect2(0f, 0f, Size));
	}
}
