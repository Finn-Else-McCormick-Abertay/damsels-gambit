using Godot;
using System;
using System.Linq;
using DamselsGambit.Util;
using System.IO;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/card.svg")]
public partial class CardDisplay : Container, ISerializationListener
{
    [Export] public string CardId {
        get;
        set {
            field = value;
            string texturePath = $"res://assets/cards/{CardId}.png";
            _cardTexture = ResourceLoader.Exists(texturePath) ? ResourceLoader.Load<Texture2D>(texturePath) : null;
            //if (_cardTexture is null) { GD.PushWarning($"No texture exists for card '{CardId}' at '{texturePath}'"); }
            UpdateCardTexture();
        }
    }
    private Texture2D _cardTexture;
    private TextureRect _textureRect;

    static Shader _roundedCornersShader = ResourceLoader.Load<Shader>("res://assets/cards/rounded_corners.gdshader");

    private void RebuildTextureRect() {
        DestroyTextureRect();
        _textureRect = new TextureRect { Texture = _cardTexture };
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
        if (_cardTexture is null || !IsInstanceValid(_textureRect)) return;
        var textureAspect = _cardTexture.GetWidth() / (float)_cardTexture.GetHeight();
        var controlAspect = Size.X / Size.Y;        
        _textureRect.Size = new Vector2(textureAspect * Size.X / controlAspect, Size.Y);
        _textureRect.Position = new Vector2(Size.X / 2f - _textureRect.Size.X / 2f, 0f);
        _textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
    }

    private void UpdateCardTexture() {
        if (!IsInstanceValid(_textureRect)) return;
        _textureRect.Texture = _cardTexture;

        var material = new ShaderMaterial { Shader = _roundedCornersShader };
        material.SetShaderParameter("radius_scale", 0.2);
        if (_cardTexture is not null) {
            var textureAspect = _cardTexture.GetWidth() / (float)_cardTexture.GetHeight();
            material.SetShaderParameter("width", textureAspect);
        }
        _textureRect.Material = material;
    }
    
    public override void _EnterTree() => RebuildTextureRect();
    public override void _ExitTree() => DestroyTextureRect();
    
    public void OnBeforeSerialize() {}
    public void OnAfterDeserialize() {
        DestroyTextureRect();
        // Clean up any stragglers from previous versions of script
        foreach (var child in this.GetInternalChildren()) { RemoveChild(child); child.QueueFree(); }
        RebuildTextureRect();
    }
    
    public override void _Notification(int what) {
        switch ((long)what) {
            case NotificationResized: { UpdateCardTransform(); } break;
            case NotificationSortChildren: { UpdateCardTransform(); } break;
        }
    }
}