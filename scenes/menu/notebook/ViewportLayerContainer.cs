using System;
using System.Collections.Generic;
using System.Linq;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/viewport_layer_container.svg")]
public partial class ViewportLayerContainer : Control, IReloadableToolScript
{
    [Export] public PackedScene[] Scenes { get; set { field = value; this.OnReady(() => UpdateViewports()); } } = [];

    [Export] public int FocusedLayer { get; set { field = value; this.OnReady(() => UpdateViewports()); } } = 0;
    
    [Export] public float LayerSeparation { get; set { field = value; this.OnReady(() => UpdateViewports()); } } = 0f;

    [Export(PropertyHint.Range, "1,179,0.1,degrees")] public float Fov { get; set { field = value; this.OnReady(() => UpdateViewports()); } } = 75f;

    [ExportGroup("Padding", "Padding")]
    [Export(PropertyHint.None, "suffix:px")] public int PaddingCamera { get; set { field = Math.Min(Math.Max(0, value), 1500); this.OnReady(() => UpdateViewports()); } }
    [Export(PropertyHint.None, "suffix:px")] public int PaddingLayer { get; set { field = Math.Max(0, value); this.OnReady(UpdateDelayed); } }

    private readonly Dictionary<string, (Control Layer, Control Root, SubViewport Viewport, Node3D SpritePivot, Sprite3D Sprite)> _layers = [];
	private Node _viewportsRoot; private SubViewport _viewport3d; private Camera3D _camera; private TextureRect _textureRect;

    public override void _ValidateProperty(Godot.Collections.Dictionary prop) {
        if (prop["name"].IsAnyOf(PropertyName._viewportsRoot, PropertyName._viewport3d, PropertyName._camera, PropertyName._textureRect))
            prop["usage"] = prop["usage"].SetFlags(PropertyUsageFlags.NoInstanceState, PropertyUsageFlags.NeverDuplicate, PropertyUsageFlags.Internal).UnsetFlags(PropertyUsageFlags.Storage);
    }

    public override void _EnterTree() {
        UpdateViewports();
        this.TryConnect(Control.SignalName.Resized, new Callable(this, MethodName.UpdateDelayed));
    }
    public override void _ExitTree() {
        ClearViewports();
        this.TryDisconnect(Control.SignalName.Resized, new Callable(this, MethodName.UpdateDelayed));
    }
    public void PreScriptReload() => ClearViewports();

    private Timer _updateDelayTimer = null;
    private void UpdateDelayed() {
        if (!this.IsValid() || !IsInsideTree()) return;
        if (!Engine.IsEditorHint()) { UpdateViewports(); return; }

        UpdateViewports(true);
        if (!_updateDelayTimer.IsValid()) {
            _updateDelayTimer = new Timer() { OneShot = true, IgnoreTimeScale = true }; AddChild(_updateDelayTimer);
            _updateDelayTimer.Connect(Timer.SignalName.Timeout, () => UpdateViewports());
        }
        _updateDelayTimer.Start(0.2);
    }

    private void UpdateViewports(bool lowImpact = false) {
        if (!IsInsideTree()) return;

        if (!_viewportsRoot.IsValid()) { _viewportsRoot = new() { Name = "ViewportsRoot" }; AddChild(_viewportsRoot); }

        if (!_textureRect.IsValid()) { _textureRect = new() { Name = "TextureRectRoot" }; AddChild(_textureRect); }
        if (!_viewport3d.IsValid()) {
            _viewport3d = new() { Name = "Viewport3D", Msaa3D = Viewport.Msaa.Msaa8X, TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.WhenParentVisible }; AddChild(_viewport3d);
            _textureRect.Texture ??= new ViewportTexture() { ViewportPath = _viewport3d.GetPath() };
        }
        if (!_camera.IsValid()) { _camera = new() { KeepAspect = Camera3D.KeepAspectEnum.Width }; _viewport3d.AddChild(_camera); }

        float spritePixelSize = 0.01f;

        _textureRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _textureRect.Position = new Vector2(-PaddingCamera, -PaddingCamera);

        _viewport3d.Size = new((int)Size.X + PaddingCamera + PaddingCamera, (int)Size.Y + PaddingCamera + PaddingCamera);
        _camera.Fov = Fov;

        var planeTrueDiameter = _viewport3d.Size.X * spritePixelSize;
        var distanceToPlane = (float)(planeTrueDiameter / (2 * Math.Tan(Angle.ToRadians(Fov)/2d)));
        
        var viewport2dSize = new Vector2I((int)Size.X + 2 * PaddingLayer, (int)Size.Y + 2 * PaddingLayer);

        var layerRootOffset = new Vector2(PaddingLayer, PaddingLayer);
        var layerSize = Size;

        void UpdateSpritePivot(Node3D pivot, int index) {
            pivot.Position = new(0f, 0f, -distanceToPlane + (index - FocusedLayer) * LayerSeparation);
        }

        foreach (var (index, layerScene) in Scenes.Index()) {
            if (!layerScene.IsValid()) continue;

            if (_layers.TryGetValue(layerScene.ResourcePath, out var tuple)) {
                if (tuple.Root.IsValid()) {
                    tuple.Root.Size = Size; tuple.Root.Position = layerRootOffset;
                    if (lowImpact) tuple.Root.Position -= new Vector2(viewport2dSize.X - tuple.Viewport.Size.X, viewport2dSize.Y - tuple.Viewport.Size.Y) / 2;
                }
                if (tuple.Viewport.IsValid() && !lowImpact) { tuple.Viewport.Size = viewport2dSize; }
                if (tuple.SpritePivot.IsValid()) UpdateSpritePivot(tuple.SpritePivot, index);
                if (tuple.Sprite.IsValid()) tuple.Sprite.SortingOffset = index * 0.01f;
            }
            else {
		        var viewport = new SubViewport() { Msaa2D = Viewport.Msaa.Msaa8X, TransparentBg = true, Disable3D = true, Size = viewport2dSize, RenderTargetUpdateMode = SubViewport.UpdateMode.WhenParentVisible };
                _viewportsRoot.AddChild(viewport);

                var layerRoot = new Control() { Position = layerRootOffset, Size = layerSize }; viewport.AddChild(layerRoot);
                var layer = layerScene.Instantiate() as Control; layerRoot.AddChild(layer);

                var spritePivot = new Node3D(); _viewport3d.AddChild(spritePivot);
                var sprite = new Sprite3D() { Texture = new ViewportTexture() { ViewportPath = viewport.GetPath() }, PixelSize = spritePixelSize }; spritePivot.AddChild(sprite);
                sprite.AlphaCut = SpriteBase3D.AlphaCutMode.Discard; sprite.AlphaAntialiasingMode = BaseMaterial3D.AlphaAntiAliasing.AlphaToCoverage; sprite.AlphaScissorThreshold = 0.93f;
                sprite.SortingOffset = index * 0.01f;
                UpdateSpritePivot(spritePivot, index);

                _layers.Add(layerScene.ResourcePath, (layer, layerRoot, viewport, spritePivot, sprite));
            }
        }

        // Clean up instances of scenes which have been removed
        foreach (var (path, (layer, root, viewport, spritePivot, sprite)) in _layers.Where(kvp => !Scenes.Where(x => x.IsValid()).Any(x => x.ResourcePath == kvp.Key))) {
            if (sprite.IsValid() && sprite.Texture.IsValid() && sprite.Texture is ViewportTexture spriteViewportTexture) spriteViewportTexture.ViewportPath = new();
            if (spritePivot.IsValid()) spritePivot.Free();
            if (viewport.IsValid()) viewport.Free();
            _layers.Remove(path);
        }

        // Final sanity check (mainly clears up things left over by reloading after the script has changed)
        foreach (var badViewport in _viewportsRoot.GetChildren().Where(x => !x.IsAnyOf(_layers.Select(x => x.Value.Viewport)))) badViewport.Free();
        foreach (var badChild in GetChildren().Where(x => !x.IsAnyOf(_viewportsRoot, _viewport3d, _camera, _textureRect, _updateDelayTimer))) badChild.Free();
    }

    private void ClearViewports() {
        foreach (var (name, (layer, root, viewport, spritePivot, sprite)) in _layers) {
            if (sprite.IsValid() && sprite.Texture.IsValid() && sprite.Texture is ViewportTexture spriteViewportTexture) spriteViewportTexture.ViewportPath = new();
            if (spritePivot.IsValid()) spritePivot.Free();
            if (root.IsValid()) root.Free();
            if (viewport.IsValid()) viewport.Free();
        }
        _layers.Clear();
        if (_textureRect.IsValid()) _textureRect.Free(); _textureRect = null;
        if (_viewport3d.IsValid()) _viewport3d.Free(); _viewport3d = null; _camera = null;
        if (_viewportsRoot.IsValid()) _viewportsRoot.Free(); _viewportsRoot = null;
        if (_updateDelayTimer.IsValid()) { _updateDelayTimer.Stop(); _updateDelayTimer.QueueFree(); _updateDelayTimer = null; }
    }
}