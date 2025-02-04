using System;
using System.Linq;
using Godot;
using Godot.Collections;
using DamselsGambit.Util;

namespace DamselsGambit;

[Tool]
public partial class AffectionMeter : Control, ISerializationListener
{
	public AffectionMeter() : base() {
		if (Theme is null) {
			Theme = new Theme();
			Theme.AddType(nameof(VSlider));
			var styleBoxEmpty = new StyleBoxEmpty();
			Theme.SetStylebox("grabber_area", nameof(VSlider), styleBoxEmpty);
			Theme.SetStylebox("grabber_area_highlight", nameof(VSlider), styleBoxEmpty);
			Theme.SetStylebox("slider", nameof(VSlider), styleBoxEmpty);
		}
	}

	[Export(PropertyHint.Range, "0,1")] public double ValuePercent { get; set { field = value; _marker?.Set(Godot.Range.PropertyName.Value, ValuePercent); } } = 0.5;
	[Export(PropertyHint.Range, "0,1")] public double LovePercent { get; set { field = value; _loveBar?.Set(Godot.Range.PropertyName.Value, LovePercent); } } = 0.3;
	[Export(PropertyHint.Range, "0,1")] public double HatePercent { get; set { field = value; _hateBar?.Set(Godot.Range.PropertyName.Value, HatePercent); } } = 0.3;

	[ExportGroup("Textures")]
	[Export] public Texture2D TextureMarker  { get; set { field = value; UpdateTextures(); } }
	[Export] public Texture2D TextureBase 	 { get; set { field = value; UpdateTextures(); } }
	[Export] public Texture2D TextureOverlay { get; set { field = value; UpdateTextures(); } }
	[Export] public Texture2D TextureLove 	 { get; set { field = value; UpdateTextures(); } }
	[Export] public Texture2D TextureHate 	 { get; set { field = value; UpdateTextures(); } }

	[Export] public bool IsNinePatch { get; set { field = value; RebuildChildren(); NotifyPropertyListChanged(); } }
	
	[ExportGroup("Patch Margin")]
	[Export(PropertyHint.None, "suffix:px")] public int PatchMarginLeft 	{ get; set { field = value; UpdateTransforms(); } }
	[Export(PropertyHint.None, "suffix:px")] public int PatchMarginTop 		{ get; set { field = value; UpdateTransforms(); } }
	[Export(PropertyHint.None, "suffix:px")] public int PatchMarginRight 	{ get; set { field = value; UpdateTransforms(); } }
	[Export(PropertyHint.None, "suffix:px")] public int PatchMarginBottom 	{ get; set { field = value; UpdateTransforms(); } }

	public override void _ValidateProperty(Dictionary propertyDict) {
		var property = propertyDict["name"].AsStringName();
		if (new[]{ PropertyName.PatchMarginLeft, PropertyName.PatchMarginTop, PropertyName.PatchMarginRight, PropertyName.PatchMarginBottom }.Contains(property)) {
			var usage = propertyDict["usage"].As<int>();
			if (!IsNinePatch) { usage &= (int)~PropertyUsageFlags.Editor; }
			propertyDict["usage"] = usage;
		}
		if (new[]{ PropertyName._base, PropertyName._overlay, PropertyName._loveBar, PropertyName._hateBar, PropertyName._marker }.Contains(property)) {
			var usage = propertyDict["usage"].As<int>();
			usage &= (int)~PropertyUsageFlags.Storage;
			usage |= (int)PropertyUsageFlags.NoInstanceState;
			propertyDict["usage"] = usage;
		}
    }

	private Control _base, _overlay;
	private Godot.Range _loveBar, _hateBar;
	private VSlider _marker;

	private void UpdateTextures() {
		Theme.SetIcon("grabber_disabled", "VSlider", TextureMarker);
		_base?.Set(TextureRect.PropertyName.Texture, TextureBase);
		_overlay?.Set(TextureRect.PropertyName.Texture, TextureOverlay);
		_loveBar?.Set(TextureProgressBar.PropertyName.TextureProgress, TextureLove);
		_hateBar?.Set(TextureProgressBar.PropertyName.TextureProgress, TextureHate);
	}

	private void UpdateTransforms() {
		foreach (var node in new[]{ _base, _overlay }) {
            if (node is NinePatchRect ninePatch) {
				ninePatch.PatchMarginLeft = PatchMarginLeft; ninePatch.PatchMarginRight = PatchMarginRight;
				ninePatch.PatchMarginTop = PatchMarginTop; ninePatch.PatchMarginBottom = PatchMarginBottom;
			}
        }
		foreach (var node in new[]{ _loveBar, _hateBar }) {
            if (node is TextureProgressBar progressBar) {
				progressBar.StretchMarginLeft = PatchMarginLeft; progressBar.StretchMarginRight = PatchMarginRight;
				progressBar.StretchMarginTop = PatchMarginTop; progressBar.StretchMarginBottom = PatchMarginBottom;
			}
        }
	}

	private void RebuildChildren() {
		ClearChildren();

		var layout = IsNinePatch ? LayoutPreset.FullRect : LayoutPreset.TopLeft;

		if (IsNinePatch) { _base = new NinePatchRect(); _overlay = new NinePatchRect(); }
		else { _base = new TextureRect(); _overlay = new TextureRect(); }
		_base.SetAnchorsAndOffsetsPreset(layout);
		_overlay.SetAnchorsAndOffsetsPreset(layout);

        _loveBar = new TextureProgressBar {
			FillMode = (int)TextureProgressBar.FillModeEnum.TopToBottom,
			NinePatchStretch = IsNinePatch,
            Value = LovePercent,
			MaxValue = 1, Step = 0.001,
        };
		_loveBar.SetAnchorsAndOffsetsPreset(layout);
		_loveBar.Value = LovePercent;

        _hateBar = new TextureProgressBar {
			FillMode = (int)TextureProgressBar.FillModeEnum.BottomToTop,
			NinePatchStretch = IsNinePatch,
            Value = HatePercent,
			MaxValue = 1, Step = 0.001,
        };
		_hateBar.SetAnchorsAndOffsetsPreset(layout);
		_hateBar.Value = LovePercent;

        _marker = new VSlider {
			Editable = false,
            Value = ValuePercent,
			MaxValue = 1, Step = 0.001,
        };
		_marker.SetAnchorsAndOffsetsPreset(layout);
		_marker.Value = ValuePercent;

        AddChild(_base, false, InternalMode.Front);
		AddChild(_hateBar, false, InternalMode.Front);
		AddChild(_loveBar, false, InternalMode.Front);
		AddChild(_overlay, false, InternalMode.Front);
		AddChild(_marker, false, InternalMode.Front);

		UpdateTextures();
		UpdateTransforms();
	}

	private void ClearChildren() {
		if (IsInstanceValid(_base)) { _base.QueueFree(); } _base = null;
		if (IsInstanceValid(_overlay)) { _overlay.QueueFree(); } _overlay = null;
		if (IsInstanceValid(_loveBar)) { _loveBar.QueueFree(); } _loveBar = null;
		if (IsInstanceValid(_hateBar)) { _hateBar.QueueFree(); } _hateBar = null;
		if (IsInstanceValid(_marker)) { _marker.QueueFree(); } _marker = null;
		//foreach (var child in this.GetInternalChildren()) { RemoveChild(child); child.QueueFree(); }
	}

	public override void _EnterTree() => RebuildChildren();
	public override void _ExitTree() => ClearChildren();

    public void OnBeforeSerialize() => ClearChildren();
    public void OnAfterDeserialize() => RebuildChildren();
}
