using System;
using System.Linq;
using Godot;
using Godot.Collections;
using DamselsGambit.Util;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/affection_meter.svg")]
public partial class AffectionMeter : Control, IReloadableToolScript
{
	[Export] public float Value { get; set { field = value; AnimateValue(value); } }
	
	[Export] public float MinValue { get; set { field = value; QueueRedraw(); } } = -10f;		[Export] public float MaxValue { get; set { field = value; QueueRedraw(); } } = 10f;
	[Export] public float LoveThreshold { get; set { field = value; QueueRedraw(); } } = 3f; 	[Export] public float HateThreshold { get; set { field = value; QueueRedraw(); } } = -3f;

	[ExportCategory("Animation")]
	[Export(PropertyHint.None, "suffix:points per second")] public double ValueAnimationSpeed { get; set; } = 1.0;
	[Export(PropertyHint.None, "suffix:seconds")] public double ValueAnimationMaxDuration { get; set; } = -1;
	[Export] public Tween.EaseType ValueAnimationEase { get; set; } = Tween.EaseType.Out;
	[Export] public Tween.TransitionType ValueAnimationTransitionType { get; set; } = Tween.TransitionType.Linear;

	private Tween _valueTween = null;
	private float AnimatedValueInternal { get; set { field = value; QueueRedraw(); } }
	private void AnimateValue(float newValue) {
		if (ValueAnimationSpeed <= 0 || ValueAnimationMaxDuration == 0 || !IsNodeReady()) { AnimatedValueInternal = newValue; return; }
		if (GodotObject.IsInstanceValid(_valueTween)) _valueTween.Kill();
		double duration = (1 / ValueAnimationSpeed * Math.Abs(AnimatedValueInternal - newValue)) switch { double val when ValueAnimationMaxDuration >= 0 => Math.Min(val, ValueAnimationMaxDuration), double val => val};
		_valueTween = CreateTween();
		_valueTween.TweenProperty(this, PropertyName.AnimatedValueInternal, newValue, duration).SetEase(Tween.EaseType.Out).SetTrans(ValueAnimationTransitionType);
	}
	
	static readonly StringName TypeName = nameof(AffectionMeter);
	public static class ThemeProperties {
		public static class Stylebox { public static readonly StringName Under = "under", Over = "over", LoveArea = "love_area", HateArea = "hate_area",
										LoveBoundary = "love_boundary", HateBoundary = "hate_boundary", MiddleBoundary = "middle_boundary", ValueBoundary = "value_boundary"; }
		public static class Icon 	 { public static readonly StringName Marker = "marker", Love = "love", Hate = "hate"; }
		public static class Constant { public static readonly StringName LoveIconSize = "love_icon_size", HateIconSize = "hate_icon_size", MarkerIconSize = "marker_icon_size"; }
	}

	public float BarLength => MaxValue - MinValue;

	// Percentage of the way down the bar (from top)
	private float ValueToPercentage(float value) => (MaxValue - value) / BarLength;

	// Distance in real units from top of bar
	private float DistanceFromTop(float value) => ValueToPercentage(value) * Size.Y;
	// Distance in real units from bottom of bar
	private float DistanceFromBottom(float value) => (1 - ValueToPercentage(value)) * Size.Y;

	public Vector2 ValueToLocalPosition(float value) => new(Size.X / 2f, DistanceFromTop(value));
	public Vector2 ValueToGlobalPosition(float value) => GetGlobalTransform() * ValueToLocalPosition(value);

	public override void _Draw() {
		Theme theme = Theme ?? this.FindParentWhere<Control>(x => x.Theme is not null)?.Theme ?? ThemeDB.GetDefaultTheme();

		void TryDrawStylebox(StringName styleboxProperty, StyleBox fallback, Rect2 rect) { if (theme.GetStyleboxOr(styleboxProperty, TypeName, fallback) is StyleBox stylebox) DrawStyleBox(stylebox, rect); }

		void TryDrawIcon(StringName iconProperty, StringName sizeProperty, Vector2 position, bool transpose = false) {
			if (theme.GetIconOrDefault(iconProperty, TypeName) is not Texture2D icon) return;
			Vector2 size = icon.GetSize() / icon.GetHeight() * theme.GetConstantOrDefault(sizeProperty, TypeName) switch { null or < 0 => icon.GetHeight(), int i => i };
			DrawTextureRect(icon, new(position.X - size.X / 2f, position.Y - size.Y / 2f, size), transpose);
		}

		TryDrawStylebox(ThemeProperties.Stylebox.LoveArea, new StyleBoxFlat{ BgColor = Colors.Red },	new(0f, 0f, Size with { Y = DistanceFromTop(LoveThreshold) }));
		TryDrawStylebox(ThemeProperties.Stylebox.HateArea, new StyleBoxFlat{ BgColor = Colors.Black }, 	new(0f, DistanceFromTop(HateThreshold), Size with { Y = DistanceFromBottom(HateThreshold) }));

		TryDrawStylebox(ThemeProperties.Stylebox.Under, ThemeDB.FallbackStylebox, new(0f, 0f, Size));
		
		TryDrawStylebox(ThemeProperties.Stylebox.LoveBoundary, null, 	new(0f, DistanceFromTop(LoveThreshold), Size.X, 0f));
		TryDrawStylebox(ThemeProperties.Stylebox.HateBoundary, null, 	new(0f, DistanceFromTop(HateThreshold), Size.X, 0f));
		TryDrawStylebox(ThemeProperties.Stylebox.MiddleBoundary, null, 	new(0f, DistanceFromTop(0), Size.X, 0f));
		
		float valueDistanceClamped = Size.Y * Math.Clamp(ValueToPercentage(AnimatedValueInternal), 0f, 1f);

		TryDrawStylebox(ThemeProperties.Stylebox.ValueBoundary, new StyleBoxLine(), new(0f, valueDistanceClamped, Size.X, 0f));
		
		TryDrawStylebox(ThemeProperties.Stylebox.Over, null, new(0f, 0f, Size));

		TryDrawIcon(ThemeProperties.Icon.Marker, ThemeProperties.Constant.MarkerIconSize, 	new(Size.X / 2f, valueDistanceClamped));
		TryDrawIcon(ThemeProperties.Icon.Love, ThemeProperties.Constant.LoveIconSize, 		new(Size.X / 2f, 0f));
		TryDrawIcon(ThemeProperties.Icon.Hate, ThemeProperties.Constant.HateIconSize, 		new(Size.X / 2f, Size.Y));
	}
}
