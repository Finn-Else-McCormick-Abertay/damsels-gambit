using Godot;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/stylebox_scaled_texture.svg")]
public partial class StyleBoxScaledTexture : StyleBox
{
	[Export] Texture2D Texture { get; set { field = value; EmitChanged(); } }
	
	[Export(PropertyHint.Range, "0,1,or_greater")] float Scale { get; set { field = value; EmitChanged(); } } = 1f;

	[Export] bool DrawCenter { get; set { field = value; EmitChanged(); } } = true;

	[ExportGroup("Texture Margins", "TextureMargin")]
	[Export(PropertyHint.None, "suffix:px")] int TextureMarginLeft { get; set { field = value; EmitChanged(); } }
	[Export(PropertyHint.None, "suffix:px")] int TextureMarginTop { get; set { field = value; EmitChanged(); } }
	[Export(PropertyHint.None, "suffix:px")] int TextureMarginRight { get; set { field = value; EmitChanged(); } }
	[Export(PropertyHint.None, "suffix:px")] int TextureMarginBottom { get; set { field = value; EmitChanged(); } }

	[ExportGroup("Expand Margins", "ExpandMargin")]
	[Export] float ExpandMarginLeft { get; set { field = value; EmitChanged(); } }
	[Export] float ExpandMarginTop { get; set { field = value; EmitChanged(); } }
	[Export] float ExpandMarginRight { get; set { field = value; EmitChanged(); } }
	[Export] float ExpandMarginBottom { get; set { field = value; EmitChanged(); } }
	
	enum AxisStretchMode { Stretch, Tile }

	[ExportGroup("Axis Stretch", "AxisStretch")]
	[Export] AxisStretchMode AxisStretchHorizontal { get; set { field = value; EmitChanged(); } } = AxisStretchMode.Stretch;
	[Export] AxisStretchMode AxisStretchVertical { get; set { field = value; EmitChanged(); } } = AxisStretchMode.Stretch;
	
	[ExportGroup("Modulate", "Modulate")]
	[Export] Color ModulateColor { get; set { field = value; EmitChanged(); } } = Colors.White;

	public override void _Draw(Rid item, Rect2 rect) {
		if (Texture is null) return;

		float textureCentreWidth = Texture.GetWidth() - TextureMarginLeft - TextureMarginRight, textureCentreHeight = Texture.GetHeight() - TextureMarginTop - TextureMarginBottom;

		// Texture regions
		var topLeftTextureRect = new Rect2(0f, 0f, TextureMarginLeft, TextureMarginTop);
		var topCentreTextureRect = new Rect2(TextureMarginLeft, 0f, textureCentreWidth, TextureMarginTop);
		var topRightTextureRect = new Rect2(Texture.GetWidth() - TextureMarginRight, 0f, TextureMarginRight, TextureMarginTop);

		var middleLeftTextureRect = new Rect2(0f, TextureMarginTop, TextureMarginLeft, textureCentreHeight);
		var middleTextureRect = new Rect2(TextureMarginLeft, TextureMarginTop, textureCentreWidth, textureCentreHeight);
		var middleRightTextureRect = new Rect2(Texture.GetWidth() - TextureMarginRight, TextureMarginTop, TextureMarginRight, textureCentreHeight);

		var bottomLeftTextureRect = new Rect2(0f, Texture.GetHeight() - TextureMarginBottom, TextureMarginLeft, TextureMarginBottom);
		var bottomCentreTextureRect = new Rect2(TextureMarginLeft, Texture.GetHeight() - TextureMarginBottom, textureCentreWidth, TextureMarginBottom);
		var bottomRightTextureRect = new Rect2(Texture.GetWidth() - TextureMarginRight, Texture.GetHeight() - TextureMarginBottom, TextureMarginRight, TextureMarginBottom);

		// Draw regions
		var fullRect = new Rect2(rect.Position - new Vector2(ExpandMarginLeft, ExpandMarginTop), rect.Size + new Vector2(ExpandMarginLeft + ExpandMarginRight, ExpandMarginTop + ExpandMarginBottom));

		float marginLeft = TextureMarginLeft * Scale, marginRight = TextureMarginRight * Scale, marginTop = TextureMarginTop * Scale, marginBottom = TextureMarginBottom * Scale;

		if ((marginLeft + marginRight) > fullRect.Size.X) { marginLeft = marginRight = fullRect.Size.X / 2f; }
		if ((marginTop + marginBottom) > fullRect.Size.Y) { marginTop = marginBottom = fullRect.Size.Y / 2f; }

		float centreWidth = fullRect.Size.X - marginLeft - marginRight, centreHeight = fullRect.Size.Y - marginTop - marginBottom;

		var topLeftRect = new Rect2(fullRect.Position, marginLeft, marginTop);
		var topCentreRect = new Rect2(fullRect.Position.X + marginLeft, fullRect.Position.Y, centreWidth, marginTop);
		var topRightRect = new Rect2(fullRect.Position.X + fullRect.Size.X - marginRight, fullRect.Position.Y, marginRight, marginTop);

		var middleLeftRect = new Rect2(fullRect.Position.X, fullRect.Position.Y + marginTop, marginLeft, centreHeight);
		var middleRect = new Rect2(fullRect.Position + new Vector2(marginLeft, marginTop), centreWidth, centreHeight);
		var middleRightRect = new Rect2(fullRect.Position.X + fullRect.Size.X - marginRight, fullRect.Position.Y + marginTop, marginRight, centreHeight);
		
		var bottomLeftRect = new Rect2(fullRect.Position.X, fullRect.Position.Y + fullRect.Size.Y - marginBottom, marginLeft, marginBottom);
		var bottomCentreRect = new Rect2(fullRect.Position.X + marginLeft, fullRect.Position.Y + fullRect.Size.Y - marginBottom, centreWidth, marginBottom);
		var bottomRightRect = new Rect2(fullRect.Position.X + fullRect.Size.X - marginRight, fullRect.Position.Y + fullRect.Size.Y - marginBottom, marginRight, marginBottom);

		void AddRect(Rect2 rect, Rect2 textureRect) => RenderingServer.CanvasItemAddTextureRectRegion(item, rect, Texture.GetRid(), textureRect, ModulateColor);
		
		if (DrawCenter) {
			AddRect(middleRect, middleTextureRect);
			// TODO - Finish tiling for centre
			/*if (AxisStretchHorizontal == AxisStretchMode.Stretch && AxisStretchVertical == AxisStretchMode.Stretch) { AddRect(middleRect, middleTextureRect); }
			if (AxisStretchHorizontal == AxisStretchMode.Stretch && AxisStretchVertical == AxisStretchMode.Tile) {

			}*/
		}

		AddRect(topLeftRect, topLeftTextureRect); AddRect(topRightRect, topRightTextureRect);
		AddRect(bottomLeftRect, bottomLeftTextureRect); AddRect(bottomRightRect, bottomRightTextureRect);
		switch (AxisStretchHorizontal) {
			case AxisStretchMode.Stretch: { AddRect(topCentreRect, topCentreTextureRect); AddRect(bottomCentreRect, bottomCentreTextureRect); } break;
			case AxisStretchMode.Tile: {
				if (Scale < 0.0001f) { GD.PushWarning("StyleBoxScaledTexture scale too small for tiling mode, failed to properly draw."); }
				else {
					var tileWidth = textureCentreWidth * Scale;
					float runningTotal = 0f;
					while ((centreWidth - runningTotal) > tileWidth) {
						AddRect(new Rect2(topCentreRect.Position.X + runningTotal, topCentreRect.Position.Y, tileWidth, topCentreRect.Size.Y), topCentreTextureRect);
						AddRect(new Rect2(bottomCentreRect.Position.X + runningTotal, bottomCentreRect.Position.Y, tileWidth, bottomCentreRect.Size.Y), bottomCentreTextureRect);
						runningTotal += tileWidth;
					}
					if (runningTotal < centreWidth) {
						float remainingWidth = centreWidth - runningTotal, remainingTextureWidth = remainingWidth / Scale;
						AddRect(
							new Rect2(topCentreRect.Position.X + runningTotal, topCentreRect.Position.Y, remainingWidth, topCentreRect.Size.Y),
							new Rect2(topCentreTextureRect.Position, remainingTextureWidth, topCentreTextureRect.Size.Y)
						);
						AddRect(
							new Rect2(bottomCentreRect.Position.X + runningTotal, bottomCentreRect.Position.Y, remainingWidth, bottomCentreRect.Size.Y),
							new Rect2(bottomCentreTextureRect.Position, remainingTextureWidth, bottomCentreTextureRect.Size.Y)
						);
					}
				}
			} break;
		}
		switch (AxisStretchVertical) {
			case AxisStretchMode.Stretch: { AddRect(middleLeftRect, middleLeftTextureRect); AddRect(middleRightRect, middleRightTextureRect); } break;
			case AxisStretchMode.Tile: {
				if (Scale < 0.0001f) { GD.PushWarning("StyleBoxScaledTexture scale too small for tiling mode, failed to properly draw."); }
				else {
					var tileHeight = textureCentreHeight * Scale;
					float runningTotal = 0f;
					while ((centreHeight - runningTotal) > tileHeight) {
						AddRect(new Rect2(middleLeftRect.Position.X, middleLeftRect.Position.Y + runningTotal, middleLeftRect.Size.X, tileHeight), middleLeftTextureRect);
						AddRect(new Rect2(middleRightRect.Position.X, middleRightRect.Position.Y + runningTotal, middleRightRect.Size.X, tileHeight), middleRightTextureRect);
						runningTotal += tileHeight;
					}
					if (runningTotal < centreHeight) {
						float remainingHeight = centreHeight - runningTotal, remainingTextureHeight = remainingHeight / Scale;
						AddRect(
							new Rect2(middleLeftRect.Position.X, middleLeftRect.Position.Y + runningTotal, middleLeftRect.Size.X, remainingHeight),
							new Rect2(middleLeftTextureRect.Position, middleLeftTextureRect.Size.X, remainingTextureHeight)
						);
						AddRect(
							new Rect2(middleRightRect.Position.X, middleRightRect.Position.Y + runningTotal, middleRightRect.Size.X, remainingHeight),
							new Rect2(middleRightTextureRect.Position, middleRightTextureRect.Size.X, remainingTextureHeight)
						);
					}
				}
			} break;
		}
	}
}
