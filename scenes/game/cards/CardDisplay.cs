using Godot;
using System;
using System.Linq;
using DamselsGambit.Util;
using System.IO;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/card.svg")]
public partial class CardDisplay : Control, IReloadableToolScript
{
	// Card Id in the form '{type}/{name}', eg: topic/witchcraft. Corresponds to the files in 'assets/cards'
	// Automatically sets texture, as well as CardType, CardName, Score, etc
	[Export] public StringName CardId {
		get; set {
			field = value;
			var id = CardId.ToString();
			var separator = id.Find('/');
			CardType = separator >= 0 ? id[..separator] : "unknown";
			CardName = separator >= 0 ? id[(separator + 1)..] : id;

			string textureRoot = $"res://assets/cards/{CardType}";

			if (ResourceLoader.Exists($"{textureRoot}/{CardName}.tres")) {
				var cardInfo = ResourceLoader.Load<CardInfo>($"{textureRoot}/{CardName}.tres");
				if (!string.IsNullOrEmpty(cardInfo.DisplayName)) DisplayName = cardInfo.DisplayName;
				Score = string.IsNullOrEmpty(cardInfo.Score) ? null : cardInfo.Score;
			}

			if (ResourceLoader.Exists($"{textureRoot}/{CardName}.png")) Texture = ResourceLoader.Load<Texture2D>($"{textureRoot}/{CardName}.png");
			else if (ResourceLoader.Exists($"{textureRoot}/template.png")) Texture = ResourceLoader.Load<Texture2D>($"{textureRoot}/template.png");
			else Texture = ThemeDB.FallbackIcon;
		}
	}
	public string CardType { get; private set {
			//GetTypeParams()?.TryDisconnect(Resource.SignalName.Changed, QueueRedraw);
			field = value; DisplayType = CardType.Capitalize();
			//GetTypeParams()?.TryConnect(Resource.SignalName.Changed, QueueRedraw);
		}
	}
	public string CardName { get; private set { field = value; DisplayName = CardName.Capitalize(); } }

	public string DisplayType { get; private set; }
	public string DisplayName { get; private set; }

	public string Score { get; private set; } = null;

	[Export] public float ShadowFalloff { get; set { field = MathF.Min(MathF.Max(value, 0f), 0.5f); RebuildMeshes(); } } = 0.1f;

	[Export] public Vector2 ShadowOffset { get; set { field = value; QueueRedraw(); } } = new();
	[Export] public float ShadowOpacity { get; set { field = value; QueueRedraw(); } } = 0.5f;

	public Texture2D Texture { get; private set { field = value; RebuildMeshes(); UpdatePivot(); (GetParent() as Container)?.QueueSort(); } }
	private static GradientTexture1D s_shadowGradientTexture;
	
	// Shared static CardSharedParams resource. Determines shared properties like corner radius
	private static CardSharedParams _sharedParams = null;
	private static CardSharedParams SharedParams {
		get {
			if (_sharedParams.IsInvalid()) {
				if (ResourceLoader.Exists("res://assets/cards/card_shared.tres")) _sharedParams = ResourceLoader.Load<CardSharedParams>("res://assets/cards/card_shared.tres") ?? new();
				else _sharedParams = new();
			}
			return _sharedParams;
		}
	}
	
	// Static cache of CardTypeParams so we only have to load each once
	private static readonly Dictionary<string, CardTypeParams> _typeParams = [];
	// Get CardTypeParams resource for current card type. Determines type properties like name position and curvature.
	private CardTypeParams GetTypeParams() {
		if (CardType.IsNullOrEmpty()) return null;
		if (_typeParams.TryGetValue(CardType, out var @params)) return @params;
		var filePath = $"res://assets/cards/{CardType}/type_params.tres";
		if (ResourceLoader.Exists(filePath)) {
			var typeParams = ResourceLoader.Load<CardTypeParams>(filePath);
			_typeParams.Add(CardType, typeParams);
			return typeParams;
		}
		return new();
	}

	// Set default focus mode & mouse cursor shape
	public CardDisplay() { MouseFilter = MouseFilterEnum.Pass; FocusMode = FocusModeEnum.All; MouseDefaultCursorShape = CursorShape.PointingHand; }

	protected virtual void PreScriptReload() {
		GetTypeParams().TryDisconnect(Resource.SignalName.Changed, QueueRedraw);
		_typeParams.Clear(); 
		_sharedParams = null;
		s_shadowGradientTexture = null;
	}

	public bool IsMousedOver { get; private set; } = false;
	private void OnMouseEntered() { IsMousedOver = true; QueueRedraw(); }
	private void OnMouseExited() { IsMousedOver = false; QueueRedraw(); }

	public Rect2 CardRect { get => new(0f, 0f, _textureAspectRatio * Size.Y, Size.Y); }
	public void UpdatePivot() => PivotOffset = Size / 2f;

	public override void _EnterTree() {
		RebuildMeshes(); UpdatePivot();
		//SharedParams?.TryConnect(Resource.SignalName.Changed, RebuildMeshes);
		this.TryConnect(CanvasItem.SignalName.ItemRectChanged, UpdatePivot);
		this.TryConnect(Control.SignalName.MouseEntered, OnMouseEntered);
		this.TryConnect(Control.SignalName.MouseExited, OnMouseExited);

		// Create shared static gradient texture used for shadows
		if (s_shadowGradientTexture is null) {
			var gradient = new Gradient{ InterpolationColorSpace = Gradient.ColorSpace.Oklab };
			gradient.SetColor(0, Colors.Black); gradient.SetColor(1, new Color(Colors.Black, 0f));
			s_shadowGradientTexture = new GradientTexture1D { Gradient = gradient };
		}
	}
	public override void _ExitTree() {
		//SharedParams?.TryDisconnect(Resource.SignalName.Changed, RebuildMeshes); 
		this.TryDisconnect(CanvasItem.SignalName.ItemRectChanged, UpdatePivot);
		this.TryDisconnect(Control.SignalName.MouseEntered, OnMouseEntered);
		this.TryDisconnect(Control.SignalName.MouseExited, OnMouseExited);
	}

	// Cached StringNames for theme property names (StringNames are strings that store a hash, making them faster to compare, but only if you cache and reuse them)
	private static readonly StringName ThemeClassName = nameof(CardDisplay);
	public static class ThemeProperties
	{
		public static class Font {
			public static readonly StringName Type = "type_font";
			public static readonly StringName Name = "name_font";
			public static readonly StringName Score = "score_font";
			public static class Size {
				public static readonly StringName Type = "type_font_size";
				public static readonly StringName Name = "name_font_size";
				public static readonly StringName Score = "score_font_size";
			}
		}
		public static class Color {
			public static readonly StringName TypeFont = "type_font_color";
			public static readonly StringName NameFont = "name_font_color";
			public static readonly StringName ScoreFont = "score_font_color";
		}
	}
	
	// Draw method. Runs when CanvasItem is redrawn
	public override void _Draw() {
		if (Texture is null) return;
		var trans = Transform2D.Identity.Scaled(new Vector2(Size.Y, Size.Y)).Translated(new Vector2((Size.X - _textureAspectRatio * Size.Y) / 2f, 0f));

		var canvasItem = GetCanvasItem();
		
		if (ShadowOpacity > 0) DrawMesh(_shadowMesh, s_shadowGradientTexture, trans.Translated(ShadowOffset.Rotated(-Rotation)), new Color(Colors.White, ShadowOpacity));
		DrawMesh(_cardMesh, Texture, trans);

		var typeParams = GetTypeParams(); //typeParams.TryConnect(Resource.SignalName.Changed, QueueRedraw);

		if (!string.IsNullOrEmpty(DisplayType)) {
			var font = GetThemeFont(ThemeProperties.Font.Type, ThemeClassName);
			var fontSize = typeParams.TypeFontSize; if (fontSize < 0) fontSize = GetThemeFontSize(ThemeProperties.Font.Size.Type, ThemeClassName);
			var fontColor = GetThemeColor(ThemeProperties.Color.TypeFont, ThemeClassName);

			DrawString(font, new Vector2(_textureAspectRatio * Size.Y / 2f * typeParams.TypePosition.X, Size.Y * typeParams.TypePosition.Y), DisplayType, HorizontalAlignment.Center, _textureAspectRatio * Size.Y, fontSize, fontColor);
		}

		if (!string.IsNullOrEmpty(DisplayName)) {
			var textServer = TextServerManager.GetPrimaryInterface();

			var font = GetThemeFont(ThemeProperties.Font.Name, ThemeClassName);
			var fontSize = typeParams.NameFontSize; if (fontSize < 0) fontSize = GetThemeFontSize(ThemeProperties.Font.Size.Name, ThemeClassName);
			var fontColor = GetThemeColor(ThemeProperties.Color.NameFont, ThemeClassName);

			if (font.IsValid()) {
				var shapedText = textServer.CreateShapedText();
				textServer.ShapedTextAddString(shapedText, DisplayName, font.GetRids(), fontSize, font.GetOpentypeFeatures(), OS.GetLocale());

				var origin = new Vector2(Size.X / 2f - _textureAspectRatio * Size.Y / 2f * typeParams.NamePosition.X - (float)textServer.ShapedTextGetWidth(shapedText) / 2f, Size.Y * typeParams.NamePosition.Y);
				
				Vector2 nextCharacterOrigin = origin;

				var glyphs = textServer.ShapedTextGetGlyphs(shapedText);
				var glyphCount = textServer.ShapedTextGetGlyphCount(shapedText);

				for (int i = 0; i < glyphCount; ++i) {
					var fontId = glyphs[i]["font_rid"].AsRid();
					var index = glyphs[i]["index"].AsInt64();
					var offset = glyphs[i]["offset"].AsVector2();
					var advance = glyphs[i]["advance"].AsDouble();

					var drawPosition = nextCharacterOrigin + offset;
					
					if (typeParams.NameCurve is not null) {
						var glyphSize = textServer.FontGetGlyphSize(fontId, new Vector2I(fontSize, fontSize), index);
						var curveOffset = typeParams.NameCurve.Sample((drawPosition.X + glyphSize.X / 2f) / (_textureAspectRatio * Size.Y));
						drawPosition.Y -= curveOffset;
					}

					textServer.FontDrawGlyph(fontId, canvasItem, fontSize, drawPosition, index, fontColor);

					if ((i + 1) < glyphCount) nextCharacterOrigin += textServer.FontGetKerning(fontId, fontSize, new Vector2I((int)index, (int)glyphs[i]["index"].AsInt64()));
					nextCharacterOrigin.X += (float)advance;
				}
				
				textServer.FreeRid(shapedText);
			}
		}

		if (!string.IsNullOrEmpty(Score)) {
			var font = GetThemeFont(ThemeProperties.Font.Score, ThemeClassName); var fontSize = GetThemeFontSize(ThemeProperties.Font.Size.Score, ThemeClassName); var fontColor = GetThemeColor(ThemeProperties.Color.ScoreFont, ThemeClassName);
			DrawString(font, new Vector2(_textureAspectRatio * Size.Y / 2f * typeParams.ScorePosition.X, Size.Y * typeParams.ScorePosition.Y), Score, HorizontalAlignment.Center, _textureAspectRatio * Size.Y, fontSize, fontColor);
		}
	}

	// Generate meshes for the card and shadow, using the shared corner radius settings
	// (Now that the corner settings are shared, I should really move the shadow falloff to the shared settings and then have this only update when
	// the shared settings change, keeping a single static instance of each mesh. I just haven't gotten round to it.)

	private float _textureAspectRatio = 0f;
	private Mesh _cardMesh, _shadowMesh;
	private void RebuildMeshes() {
		_textureAspectRatio = Texture is not null ? Texture.GetHeight() > 0f ? Texture.GetWidth() / (float)Texture.GetHeight() : 0f : 0f;
		if (_textureAspectRatio == 0f) { _cardMesh = _shadowMesh = null; return; }

		SurfaceTool cardSt = new(), shadowSt = new();
		cardSt.Begin(Mesh.PrimitiveType.Triangles); cardSt.SetColor(Colors.White);
		shadowSt.Begin(Mesh.PrimitiveType.Triangles); shadowSt.SetColor(Colors.White);
		
		void AddRect(SurfaceTool st, float x, float y, float width, float height, float? aU = null, float? bU = null, float? cU = null, float? dU = null) {
			Vector2 a = new(x * _textureAspectRatio, y), b = new((x + width) * _textureAspectRatio, y), c = new((x + width) * _textureAspectRatio, y + height), d = new(x * _textureAspectRatio, y + height);
			Vector2 aUV, bUV, cUV, dUV;
			if (aU is not null || bU is not null || cU is not null || dU is not null) { aUV = new(aU ?? 0f, 0f); bUV = new(bU ?? 0f, 0f); cUV = new(cU ?? 0f, 0f); dUV = new(dU ?? 0f, 0f); }
			else { aUV = new(x, y); bUV = new(x + width, y); cUV = new(x + width, y + height); dUV = new(x, y + height); }
			st.AddQuad2D(a, b, c, d, aUV, bUV, cUV, dUV);
		}
		void AddCorner(SurfaceTool st, Vector2 origin, float startAngle, float? innerRadius = null, float? uStart = null, float? uEnd = null) {
			float wedgeRadius = innerRadius ?? SharedParams.CornerRadius;
			
			Vector2? gradUvStart = uStart is not null ? new((float)uStart, 0f) : null;
			Vector2? gradUvEnd = uEnd is not null ? new((float)uEnd, 0f) : null;

			Vector2 a = new(origin.X * _textureAspectRatio, origin.Y);
			Vector2 aUV = gradUvStart ?? origin;

			float angleIncrement = MathF.PI / 2 / SharedParams.CornerResolution;
			for (int i = 0; i < SharedParams.CornerResolution; ++i) {
				var angle = i * angleIncrement + startAngle;
				var angleNext = (i + 1) * angleIncrement + startAngle;

				float cosine = MathF.Cos(angle), sine = MathF.Sin(angle);
				float cosineNext = MathF.Cos(angleNext), sineNext = MathF.Sin(angleNext);

				Vector2 b = new Vector2(wedgeRadius / _textureAspectRatio * cosine, wedgeRadius * sine) + origin;
				Vector2 bUV = (innerRadius is not null ? gradUvStart : gradUvEnd) ?? b;
				b.X *= _textureAspectRatio;

				Vector2 c = new Vector2(wedgeRadius / _textureAspectRatio * cosineNext, wedgeRadius * sineNext) + origin;
				Vector2 cUV = (innerRadius is not null ? gradUvStart : gradUvEnd) ?? c;
				c.X *= _textureAspectRatio;
				
				st.AddTri2D(a, b, c, aUV, bUV, cUV);

				if (innerRadius is not null) {
					Vector2 d = new Vector2(SharedParams.CornerRadius / _textureAspectRatio * cosine, SharedParams.CornerRadius * sine) + origin;
					Vector2 dUV = gradUvEnd ?? d;
					d.X *= _textureAspectRatio;

					Vector2 e = new Vector2(SharedParams.CornerRadius / _textureAspectRatio * cosineNext, SharedParams.CornerRadius * sineNext) + origin;
					Vector2 eUV = gradUvEnd ?? e;
					e.X *= _textureAspectRatio;

					st.AddQuad2D(c, b, d, e, cUV, bUV, dUV, eUV);
				}
			}
		}

		// Create Card
		AddRect(cardSt, 0, SharedParams.CornerRadius, 1, 1 - SharedParams.CornerRadius * 2); 																			// Centre quad
		AddRect(cardSt, SharedParams.CornerRadius / _textureAspectRatio, 0f, 1 - SharedParams.CornerRadius / _textureAspectRatio * 2, SharedParams.CornerRadius); 		// Top quad
		AddRect(cardSt, SharedParams.CornerRadius / _textureAspectRatio, 1 - SharedParams.CornerRadius, 1 - SharedParams.CornerRadius / _textureAspectRatio * 2, SharedParams.CornerRadius); 	// Bottom quad

		AddCorner(cardSt, new(SharedParams.CornerRadius / _textureAspectRatio, SharedParams.CornerRadius), MathF.PI); 													// Top left corner
		AddCorner(cardSt, new(1 - SharedParams.CornerRadius / _textureAspectRatio, SharedParams.CornerRadius), MathF.PI * 1.5f); 										// Top right corner
		AddCorner(cardSt, new(SharedParams.CornerRadius / _textureAspectRatio, 1 - SharedParams.CornerRadius), MathF.PI * 0.5f); 										// Bottom left corner
		AddCorner(cardSt, new(1 - SharedParams.CornerRadius / _textureAspectRatio, 1 - SharedParams.CornerRadius), 0f); 												// Bottom right corner

		// Create Shadow
		float? innerRadius = SharedParams.CornerRadius > ShadowFalloff ? SharedParams.CornerRadius - ShadowFalloff : null;
		float gradCornerStart = SharedParams.CornerRadius > ShadowFalloff ? 0f : (ShadowFalloff - SharedParams.CornerRadius) / ShadowFalloff;
		
		// Centre quad
		if (SharedParams.CornerRadius > ShadowFalloff) {
			AddRect(shadowSt, SharedParams.CornerRadius / _textureAspectRatio, ShadowFalloff, 1 - SharedParams.CornerRadius / _textureAspectRatio * 2, 1 - ShadowFalloff * 2, 0f, 0f, 0f, 0f);
		}
		else {
			Vector2 startUV = new(gradCornerStart, 0f), endUV = new(0f, 0f);

			shadowSt.AddTri2D(new(SharedParams.CornerRadius, SharedParams.CornerRadius), new(0.5f * _textureAspectRatio, SharedParams.CornerRadius), new(0.5f * _textureAspectRatio, 0.5f), startUV, startUV, endUV);
			shadowSt.AddTri2D(new(SharedParams.CornerRadius, SharedParams.CornerRadius), new(0.5f * _textureAspectRatio, 0.5f), new(SharedParams.CornerRadius, 0.5f), startUV, endUV, startUV);

			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(_textureAspectRatio - SharedParams.CornerRadius, 0.5f), new(_textureAspectRatio - SharedParams.CornerRadius, SharedParams.CornerRadius), endUV, startUV, startUV);
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(0.5f * _textureAspectRatio, SharedParams.CornerRadius), new(_textureAspectRatio - SharedParams.CornerRadius, SharedParams.CornerRadius), endUV, startUV, startUV);
			
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(_textureAspectRatio - SharedParams.CornerRadius, 0.5f), new(_textureAspectRatio - SharedParams.CornerRadius, 1 - SharedParams.CornerRadius), endUV, startUV, startUV);
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(_textureAspectRatio - SharedParams.CornerRadius, 1 - SharedParams.CornerRadius), new(0.5f * _textureAspectRatio, 1 - SharedParams.CornerRadius), endUV, startUV, startUV);
			
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(0.5f * _textureAspectRatio, 1 - SharedParams.CornerRadius), new(SharedParams.CornerRadius, 1 - SharedParams.CornerRadius), endUV, startUV, startUV);
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(SharedParams.CornerRadius, 1 - SharedParams.CornerRadius), new(SharedParams.CornerRadius, 0.5f), endUV, startUV, startUV);
		}
		AddRect(shadowSt, SharedParams.CornerRadius / _textureAspectRatio, 0f, 1 - SharedParams.CornerRadius / _textureAspectRatio * 2, MathF.Min(SharedParams.CornerRadius, ShadowFalloff), 1f, 1f, gradCornerStart, gradCornerStart);
		AddRect(shadowSt, SharedParams.CornerRadius / _textureAspectRatio, 1 - MathF.Min(SharedParams.CornerRadius, ShadowFalloff), 1 - SharedParams.CornerRadius / _textureAspectRatio * 2, MathF.Min(SharedParams.CornerRadius, ShadowFalloff), gradCornerStart, gradCornerStart, 1f, 1f);
		AddRect(shadowSt, 0f, SharedParams.CornerRadius, MathF.Min(SharedParams.CornerRadius, ShadowFalloff) / _textureAspectRatio, 1 - SharedParams.CornerRadius * 2, 1f, gradCornerStart, gradCornerStart, 1f);
		AddRect(shadowSt, 1 - MathF.Min(SharedParams.CornerRadius, ShadowFalloff) / _textureAspectRatio, SharedParams.CornerRadius, MathF.Min(SharedParams.CornerRadius, ShadowFalloff) / _textureAspectRatio, 1 - SharedParams.CornerRadius * 2, gradCornerStart, 1f, 1f, gradCornerStart);
		
		if (innerRadius is not null) {
			AddRect(shadowSt, ShadowFalloff / _textureAspectRatio, SharedParams.CornerRadius, (SharedParams.CornerRadius - ShadowFalloff) / _textureAspectRatio, 1 - SharedParams.CornerRadius * 2, 0f, 0f, 0f, 0f);
			AddRect(shadowSt, 1 - SharedParams.CornerRadius / _textureAspectRatio, SharedParams.CornerRadius, (SharedParams.CornerRadius - ShadowFalloff) / _textureAspectRatio, 1 - SharedParams.CornerRadius * 2, 0f, 0f, 0f, 0f);
		}
		
		AddCorner(shadowSt, new(SharedParams.CornerRadius / _textureAspectRatio, SharedParams.CornerRadius), MathF.PI, innerRadius, gradCornerStart, 1f);				// Top left corner
		AddCorner(shadowSt, new(1 - SharedParams.CornerRadius / _textureAspectRatio, SharedParams.CornerRadius), MathF.PI * 1.5f, innerRadius, gradCornerStart, 1f); 	// Top right corner
		AddCorner(shadowSt, new(SharedParams.CornerRadius / _textureAspectRatio, 1 - SharedParams.CornerRadius), MathF.PI * 0.5f, innerRadius, gradCornerStart, 1f);	// Bottom left corner
		AddCorner(shadowSt, new(1 - SharedParams.CornerRadius / _textureAspectRatio, 1 - SharedParams.CornerRadius), 0f, innerRadius, gradCornerStart, 1f);			// Bottom right corner

		_cardMesh = cardSt.Commit();
		_shadowMesh = shadowSt.Commit();

		QueueRedraw();
	}
}
