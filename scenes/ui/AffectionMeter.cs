using Godot;

[Tool]
public partial class AffectionMeter : Control
{
	private double _valuePercent = 0.5, _lovePercent = 0.3, _hatePercent = 0.3;
	[Export] public double ValuePercent { get => _valuePercent; set { _valuePercent = value; marker?.Set(Range.PropertyName.Value, ValuePercent); } }
	[Export] public double LovePercent { get => _lovePercent; set { _lovePercent = value; barLove?.Set(Range.PropertyName.Value, LovePercent); } }
	[Export] public double HatePercent { get => _hatePercent; set { _hatePercent = value; barHate?.Set(Range.PropertyName.Value, HatePercent); } }

	[ExportGroup("Textures")]
	private Texture2D _barBaseTexture, _barLoveTexture, _barHateTexture, _markerTexture;
	[Export] Texture2D BarBaseTexture { get => _barBaseTexture; set { _barBaseTexture = value; barBase?.Set(NinePatchRect.PropertyName.Texture, _barBaseTexture); } }
	[Export] Texture2D BarLoveTexture { get => _barLoveTexture; set { _barLoveTexture = value; barLove?.Set(TextureProgressBar.PropertyName.TextureProgress, _barLoveTexture); } }
	[Export] Texture2D BarHateTexture { get => _barHateTexture; set { _barHateTexture = value; barHate?.Set(TextureProgressBar.PropertyName.TextureProgress, _barHateTexture); } }
	[Export] Texture2D MarkerTexture { get => _markerTexture; set { _markerTexture = value; Theme.SetIcon("grabber_disabled", "VSlider", _markerTexture); } }

	private NinePatchRect barBase;
	private TextureProgressBar barLove, barHate;
	private VSlider marker;

	public override void _Ready() {
		barBase = GetNode<NinePatchRect>("BarBase");
		barLove = GetNode<TextureProgressBar>("LoveBar");
		barHate = GetNode<TextureProgressBar>("HateBar");
		marker = GetNode<VSlider>("VSlider");

		// Update to exported values
		ValuePercent = _valuePercent;
		LovePercent = _lovePercent;
		HatePercent = _hatePercent;

		BarBaseTexture = _barBaseTexture;
		BarLoveTexture = _barLoveTexture;
		BarHateTexture = _barHateTexture;
	}
}
