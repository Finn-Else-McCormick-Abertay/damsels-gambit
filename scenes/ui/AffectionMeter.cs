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
			public static readonly StringName Love = "love";
			public static readonly StringName Hate = "hate";
		}
		public static class Icon {
			public static readonly StringName Marker = "marker";
		}
	}

	public override void _Draw() {
		var under = Theme?.TryGetStylebox(ThemeProperties.Stylebox.Under, TypeName) ?? ThemeDB.FallbackStylebox;
		var over = Theme?.TryGetStylebox(ThemeProperties.Stylebox.Over, TypeName) ?? new StyleBoxEmpty();
		var love = Theme?.TryGetStylebox(ThemeProperties.Stylebox.Love, TypeName) ?? new StyleBoxFlat{ BgColor = Colors.Red };
		var hate = Theme?.TryGetStylebox(ThemeProperties.Stylebox.Hate, TypeName) ?? new StyleBoxFlat{ BgColor = Colors.Black };
		var marker = Theme?.TryGetIcon(ThemeProperties.Icon.Marker, TypeName) ?? ThemeDB.FallbackIcon;

		DrawStyleBox(under, new Rect2(0f, 0f, Size));
		DrawStyleBox(love, new Rect2(0f, 0f, Size with { Y = Size.Y * LovePercent }));
		DrawStyleBox(hate, new Rect2(0f, (1 - HatePercent) * Size.Y, Size with { Y = Size.Y * HatePercent }));
		DrawTexture(marker, new(Size.X / 2f - marker.GetWidth() / 2f, Size.Y * ValuePercent - marker.GetHeight() / 2f));
		DrawStyleBox(over, new Rect2(0f, 0f, Size));
	}
}
