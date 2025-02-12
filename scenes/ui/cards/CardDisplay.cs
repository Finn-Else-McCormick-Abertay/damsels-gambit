using Godot;
using System;
using System.Linq;
using DamselsGambit.Util;
using System.IO;
using System.Collections.Generic;
using Godot.Collections;
namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/card.svg")]
public partial class CardDisplay : Control, IReloadableToolScript
{
	[Export] public StringName CardId {
		get; set {
			field = value;
			var id = CardId.ToString();
			var separator = id.Find('/');
			var type = separator >= 0 ? id[..(separator)] : "unknown";
			var name = separator >= 0 ? id[(separator + 1)..] : id;

			DisplayName = name.Capitalize();

			string textureRoot = $"res://assets/cards/{type}";
			if (ResourceLoader.Exists($"{textureRoot}/{name}.png")) {
				Texture = ResourceLoader.Load<Texture2D>($"{textureRoot}/{name}.png");
				_renderName = false;
			}
			else {
				_renderName = true;
				if (ResourceLoader.Exists($"{textureRoot}/template.png")) { Texture = ResourceLoader.Load<Texture2D>($"{textureRoot}/template.png"); }
				else { Texture = ThemeDB.FallbackIcon; }
			}
		}
	}
	public string DisplayName { get; private set; }
	private bool _renderName = false;

	[Export(PropertyHint.Range, "0,0.5,")] public float CornerRadius { get; set { field = value; RebuildMeshes(); } } = 0.15f;
	[Export] public int CornerResolution { get; set { field = Math.Max(value, 1); RebuildMeshes(); } } = 10;

	[Export] public float ShadowFalloff { get; set { field = MathF.Min(MathF.Max(value, 0f), 0.5f); RebuildMeshes(); } } = 0.1f;

	[Export] public Vector2 ShadowOffset { get; set { field = value; QueueRedraw(); } } = new();
	[Export] public float ShadowOpacity { get; set { field = value; QueueRedraw(); } } = 0.5f;

	public Texture2D Texture { get; private set { field = value; RebuildMeshes(); UpdatePivot(); (GetParent() as Container)?.QueueSort(); } }
	private static readonly GradientTexture1D s_shadowGradientTexture;

	static CardDisplay() {
		var gradient = new Gradient{ InterpolationColorSpace = Gradient.ColorSpace.Oklab };
		gradient.SetColor(0, Colors.Black); gradient.SetColor(1, new Color(Colors.Black, 0f));
		s_shadowGradientTexture = new GradientTexture1D { Gradient = gradient };
	}

	public bool IsMousedOver = false;
	private void OnMouseEntered() { IsMousedOver = true; QueueRedraw(); }
	private void OnMouseExited() { IsMousedOver = false; QueueRedraw(); }

	public override void _EnterTree() {
		RebuildMeshes(); UpdatePivot();
		this.TryConnect(SignalName.ItemRectChanged, new Callable(this, MethodName.UpdatePivot));
		this.TryConnect(SignalName.MouseEntered, new Callable(this, MethodName.OnMouseEntered));
		this.TryConnect(SignalName.MouseExited, new Callable(this, MethodName.OnMouseExited));
	}
	public override void _ExitTree() {
		this.TryDisconnect(SignalName.ItemRectChanged, new Callable(this, MethodName.UpdatePivot));
		this.TryDisconnect(SignalName.MouseEntered, new Callable(this, MethodName.OnMouseEntered));
		this.TryDisconnect(SignalName.MouseExited, new Callable(this, MethodName.OnMouseExited));
	}

	public void UpdatePivot() { PivotOffset = Size / 2f; }

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
			float wedgeRadius = innerRadius ?? CornerRadius;
			
			Vector2? gradUvStart = uStart is not null ? new((float)uStart, 0f) : null;
			Vector2? gradUvEnd = uEnd is not null ? new((float)uEnd, 0f) : null;

			Vector2 a = new(origin.X * _textureAspectRatio, origin.Y);
			Vector2 aUV = gradUvStart ?? origin;

			float angleIncrement = MathF.PI / 2 / CornerResolution;
			for (int i = 0; i < CornerResolution; ++i) {
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
					Vector2 d = new Vector2(CornerRadius / _textureAspectRatio * cosine, CornerRadius * sine) + origin;
					Vector2 dUV = gradUvEnd ?? d;
					d.X *= _textureAspectRatio;

					Vector2 e = new Vector2(CornerRadius / _textureAspectRatio * cosineNext, CornerRadius * sineNext) + origin;
					Vector2 eUV = gradUvEnd ?? e;
					e.X *= _textureAspectRatio;

					st.AddQuad2D(c, b, d, e, cUV, bUV, dUV, eUV);
				}
			}
		}

		// Create Card
		AddRect(cardSt, 0, CornerRadius, 1, 1 - CornerRadius * 2); 																			// Centre quad
		AddRect(cardSt, CornerRadius / _textureAspectRatio, 0f, 1 - CornerRadius / _textureAspectRatio * 2, CornerRadius); 				 	// Top quad
		AddRect(cardSt, CornerRadius / _textureAspectRatio, 1 - CornerRadius, 1 - CornerRadius / _textureAspectRatio * 2, CornerRadius); 	// Bottom quad

		AddCorner(cardSt, new(CornerRadius / _textureAspectRatio, CornerRadius), MathF.PI); 												// Top left corner
		AddCorner(cardSt, new(1 - CornerRadius / _textureAspectRatio, CornerRadius), MathF.PI * 1.5f); 										// Top right corner
		AddCorner(cardSt, new(CornerRadius / _textureAspectRatio, 1 - CornerRadius), MathF.PI * 0.5f); 										// Bottom left corner
		AddCorner(cardSt, new(1 - CornerRadius / _textureAspectRatio, 1 - CornerRadius), 0f); 												// Bottom right corner

		// Create Shadow
		float? innerRadius = CornerRadius > ShadowFalloff ? CornerRadius - ShadowFalloff : null;
		float gradCornerStart = CornerRadius > ShadowFalloff ? 0f : (ShadowFalloff - CornerRadius) / ShadowFalloff;
		
		// Centre quad
		if (CornerRadius > ShadowFalloff) {
			AddRect(shadowSt, CornerRadius / _textureAspectRatio, ShadowFalloff, 1 - CornerRadius / _textureAspectRatio * 2, 1 - ShadowFalloff * 2, 0f, 0f, 0f, 0f);
		}
		else {
			Vector2 startUV = new(gradCornerStart, 0f), endUV = new(0f, 0f);

			shadowSt.AddTri2D(new(CornerRadius, CornerRadius), new(0.5f * _textureAspectRatio, CornerRadius), new(0.5f * _textureAspectRatio, 0.5f), startUV, startUV, endUV);
			shadowSt.AddTri2D(new(CornerRadius, CornerRadius), new(0.5f * _textureAspectRatio, 0.5f), new(CornerRadius, 0.5f), startUV, endUV, startUV);

			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(_textureAspectRatio - CornerRadius, 0.5f), new(_textureAspectRatio - CornerRadius, CornerRadius), endUV, startUV, startUV);
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(0.5f * _textureAspectRatio, CornerRadius), new(_textureAspectRatio - CornerRadius, CornerRadius), endUV, startUV, startUV);
			
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(_textureAspectRatio - CornerRadius, 0.5f), new(_textureAspectRatio - CornerRadius, 1 - CornerRadius), endUV, startUV, startUV);
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(_textureAspectRatio - CornerRadius, 1 - CornerRadius), new(0.5f * _textureAspectRatio, 1 - CornerRadius), endUV, startUV, startUV);
			
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(0.5f * _textureAspectRatio, 1 - CornerRadius), new(CornerRadius, 1 - CornerRadius), endUV, startUV, startUV);
			shadowSt.AddTri2D(new(0.5f * _textureAspectRatio, 0.5f), new(CornerRadius, 1 - CornerRadius), new(CornerRadius, 0.5f), endUV, startUV, startUV);
		}
		AddRect(shadowSt, CornerRadius / _textureAspectRatio, 0f, 1 - CornerRadius / _textureAspectRatio * 2, MathF.Min(CornerRadius, ShadowFalloff), 1f, 1f, gradCornerStart, gradCornerStart);
		AddRect(shadowSt, CornerRadius / _textureAspectRatio, 1 - MathF.Min(CornerRadius, ShadowFalloff), 1 - CornerRadius / _textureAspectRatio * 2, MathF.Min(CornerRadius, ShadowFalloff), gradCornerStart, gradCornerStart, 1f, 1f);
		AddRect(shadowSt, 0f, CornerRadius, MathF.Min(CornerRadius, ShadowFalloff) / _textureAspectRatio, 1 - CornerRadius * 2, 1f, gradCornerStart, gradCornerStart, 1f);
		AddRect(shadowSt, 1 - MathF.Min(CornerRadius, ShadowFalloff) / _textureAspectRatio, CornerRadius, MathF.Min(CornerRadius, ShadowFalloff) / _textureAspectRatio, 1 - CornerRadius * 2, gradCornerStart, 1f, 1f, gradCornerStart);
		
		if (innerRadius is not null) {
			AddRect(shadowSt, ShadowFalloff / _textureAspectRatio, CornerRadius, (CornerRadius - ShadowFalloff) / _textureAspectRatio, 1 - CornerRadius * 2, 0f, 0f, 0f, 0f);
			AddRect(shadowSt, 1 - CornerRadius / _textureAspectRatio, CornerRadius, (CornerRadius - ShadowFalloff) / _textureAspectRatio, 1 - CornerRadius * 2, 0f, 0f, 0f, 0f);
		}
		
		AddCorner(shadowSt, new(CornerRadius / _textureAspectRatio, CornerRadius), MathF.PI, innerRadius, gradCornerStart, 1f);				// Top left corner
		AddCorner(shadowSt, new(1 - CornerRadius / _textureAspectRatio, CornerRadius), MathF.PI * 1.5f, innerRadius, gradCornerStart, 1f); 	// Top right corner
		AddCorner(shadowSt, new(CornerRadius / _textureAspectRatio, 1 - CornerRadius), MathF.PI * 0.5f, innerRadius, gradCornerStart, 1f);	// Bottom left corner
		AddCorner(shadowSt, new(1 - CornerRadius / _textureAspectRatio, 1 - CornerRadius), 0f, innerRadius, gradCornerStart, 1f);			// Bottom right corner

		_cardMesh = cardSt.Commit();
		_shadowMesh = shadowSt.Commit();

		QueueRedraw();
	}

	public override void _Draw() {
		if (Texture is null) { return; }
		var trans = Transform2D.Identity.Scaled(new Vector2(Size.Y, Size.Y)).Translated(new Vector2((Size.X - _textureAspectRatio * Size.Y) / 2f, 0f));
		
		if (ShadowOpacity > 0) { DrawMesh(_shadowMesh, s_shadowGradientTexture, trans.Translated(ShadowOffset.Rotated(-Rotation)), new Color(Colors.White, ShadowOpacity)); }
		DrawMesh(_cardMesh, Texture, trans);

		if (_renderName) {
			DrawString(ThemeDB.GetDefaultTheme().DefaultFont, Size / 2f - new Vector2(_textureAspectRatio * Size.Y / 2f, 0f), DisplayName, HorizontalAlignment.Center, _textureAspectRatio * Size.Y, 18, Colors.Black);
		}
	}
}
