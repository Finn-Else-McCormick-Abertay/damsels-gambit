using Godot;
using System;
using System.Linq;
using DamselsGambit.Util;
using System.IO;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/card.svg")]
public partial class CardDisplay : Container, IReloadableToolScript
{
	[Export] public string CardId {
		get;
		set {
			field = value;
			string texturePath = $"res://assets/cards/{CardId}.png";
			CardTexture = ResourceLoader.Exists(texturePath) ? ResourceLoader.Load<Texture2D>(texturePath) : null;
			//if (_cardTexture is null) { GD.PushWarning($"No texture exists for card '{CardId}' at '{texturePath}'"); }
		}
	}
	public Texture2D CardTexture { get; private set { field = value; UpdateCardTexture(); } }

	private TextureRect _textureRect;

	static Shader _roundedCornersShader = ResourceLoader.Load<Shader>("res://assets/cards/rounded_corners.gdshader");

	private void RebuildTextureRect() {
		DestroyTextureRect();
		_textureRect = new TextureRect { Texture = CardTexture };
		AddChild(_textureRect, false, InternalMode.Back);
		if (!Engine.IsEditorHint()) { _textureRect.Owner = this; }
		UpdateCardTransform();
		UpdateCardTexture();
	}

	private void DestroyTextureRect() {
		if (IsInstanceValid(_textureRect)) { _textureRect.QueueFree(); }
		_textureRect = null;
	}

	private void UpdateCardTransform() {
		if (CardTexture is null || !IsInstanceValid(_textureRect)) return;
		var textureAspect = CardTexture.GetWidth() / (float)CardTexture.GetHeight();
		var controlAspect = Size.Y > 0f ? Size.X / Size.Y : 0f;
		_textureRect.Size = new Vector2(controlAspect > 0f ? textureAspect * Size.X / controlAspect : 0f, Size.Y);
		_textureRect.Position = new Vector2(Size.X / 2f - _textureRect.Size.X / 2f, 0f);
		_textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
	}

	private void UpdateCardTexture() {
		if (!IsInstanceValid(_textureRect)) return;
		_textureRect.Texture = CardTexture;

		var material = new ShaderMaterial { Shader = _roundedCornersShader };
		material.SetShaderParameter("radius_scale", 0.2);
		if (CardTexture is not null) {
			var textureAspect = CardTexture.GetWidth() / (float)CardTexture.GetHeight();
			material.SetShaderParameter("width", textureAspect);
		}
		_textureRect.Material = material;
	}
	
	public override void _EnterTree() => RebuildTextureRect();
	public override void _ExitTree() => DestroyTextureRect();
	// Clean up any stragglers from previous versions of script
	protected void OnScriptReload() { foreach (var child in this.GetInternalChildren()) { RemoveChild(child); child.QueueFree(); } }
	
	public override void _Notification(int what) {
		switch ((long)what) {
			case NotificationSortChildren:
			case NotificationResized: { UpdateCardTransform(); } break;
		}
	}
}
