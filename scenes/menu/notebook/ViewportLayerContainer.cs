using System.Collections.Generic;
using System.Linq;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/viewport_layer_container.svg")]
public partial class ViewportLayerContainer : Control, IReloadableToolScript
{
    [Export] private PackedScene[] Scenes { get; set { field = value; this.OnReady(UpdateViewports); } } = [];

    [ExportGroup("Padding", "Padding")]
    [Export] public int PaddingLeft { get; set { field = value; this.OnReady(UpdateViewports); } } = 0;
    [Export] public int PaddingTop { get; set { field = value; this.OnReady(UpdateViewports); } } = 0;
    [Export] public int PaddingRight { get; set { field = value; this.OnReady(UpdateViewports); } } = 0;
    [Export] public int PaddingBottom { get; set { field = value; this.OnReady(UpdateViewports); } } = 0;

	private Node _viewportsRoot; private Control _textureRectRoot;
    private readonly Dictionary<string, (Control Layer, Control Root, SubViewport Viewport, TextureRect TextureRect, ViewportTexture Texture)> _layers = [];

    public override void _ValidateProperty(Godot.Collections.Dictionary prop) {
        if (prop["name"].IsAnyOf(PropertyName._viewportsRoot, PropertyName._textureRectRoot))
            prop["usage"] = prop["usage"].SetFlags(PropertyUsageFlags.NoInstanceState, PropertyUsageFlags.NeverDuplicate, PropertyUsageFlags.Internal).UnsetFlags(PropertyUsageFlags.Storage);
    }

    public override void _EnterTree() {
        UpdateViewports();
        this.TryConnect(Control.SignalName.Resized, new Callable(this, MethodName.UpdateViewports));
    }
    public override void _ExitTree() {
        ClearViewports();
        this.TryDisconnect(Control.SignalName.Resized, new Callable(this, MethodName.UpdateViewports));
    }
    public void PreScriptReload() => ClearViewports();

    private void UpdateViewports() {
        // For reasons unclear to me, when the scene is first opened in the editor _EnterTree seems to run before it's actually in the tree.
        if (!IsInsideTree()) return;

        if (!_viewportsRoot.IsValid()) { _viewportsRoot = new() { Name = "ViewportsRoot" }; AddChild(_viewportsRoot); }
        if (!_textureRectRoot.IsValid()) { _textureRectRoot = new() { Name = "TextureRectRoot" }; AddChild(_textureRectRoot); }
        _textureRectRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Cleanup anything which has become invalid for one reason or another
        foreach (var (name, (layer, root, viewport, rect, texture)) in _layers) {
            if (layer.IsValid() && root.IsValid() && viewport.IsValid() && rect.IsValid() && texture.IsValid()) continue;
            Console.Warning($"Invalid Layer '{name}'"); if (root.IsValid()) root.QueueFree(); if (viewport.IsValid()) viewport.QueueFree(); if (rect.IsValid()) rect.QueueFree(); _layers.Remove(name);
        }

        var rootOffset = new Vector2(PaddingLeft, PaddingTop);
        var viewportSize = new Vector2I((int)Size.X + PaddingLeft + PaddingRight, (int)Size.Y + PaddingTop + PaddingBottom);

        foreach (var (index, layerScene) in Scenes.Index()) {
            if (!layerScene.IsValid()) continue;

            if (_layers.TryGetValue(layerScene.ResourcePath, out var tuple)) {
                if (tuple.Root.IsValid()) { tuple.Root.Size = Size; tuple.Root.Position = rootOffset; }
                if (tuple.Layer.IsValid()) { tuple.Layer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); }
                if (tuple.Viewport.IsValid()) tuple.Viewport.Size = viewportSize;
                if (tuple.TextureRect.IsValid()) { tuple.TextureRect.Position = rootOffset * -1; tuple.TextureRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); tuple.TextureRect.ZIndex = index; }
            }
            else {
		        var viewport2d = new SubViewport() { Msaa2D = Viewport.Msaa.Msaa8X, TransparentBg = true, Disable3D = true, Size = viewportSize }; _viewportsRoot.AddChild(viewport2d);

                var layerRoot = new Control() { Position = rootOffset, Size = Size }; viewport2d.AddChild(layerRoot);
                var layer = layerScene.Instantiate() as Control; layerRoot.AddChild(layer); layer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

                var viewportTexture = new ViewportTexture() { ViewportPath = viewport2d.GetPath() };
                var textureRect = new TextureRect() { Position = rootOffset * -1, Texture = viewportTexture }; _textureRectRoot.AddChild(textureRect); textureRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                textureRect.ZIndex = index;

                _layers.Add(layerScene.ResourcePath, (layer, layerRoot, viewport2d, textureRect, viewportTexture));
            }
        }

        // Clean up instances of scenes which have been removed
        foreach (var (path, tuple) in _layers.Where(kvp => !Scenes.Where(x => x.IsValid()).Any(x => x.ResourcePath == kvp.Key))) {
            if (tuple.Texture.IsValid()) tuple.Texture.ViewportPath = new();
            if (tuple.TextureRect.IsValid()) tuple.TextureRect.QueueFree();
            if (tuple.Viewport.IsValid()) tuple.Viewport.QueueFree();
            _layers.Remove(path);
        }

        // Final sanity check (mainly clears up things left over by reloading after the script has changed)
        foreach (var badViewport in _viewportsRoot.GetChildren().Where(x => !x.IsAnyOf(_layers.Select(x => x.Value.Viewport)))) badViewport.QueueFree();
        foreach (var badTextureRect in _textureRectRoot.GetChildren().Where(x => !x.IsAnyOf(_layers.Select(x => x.Value.TextureRect)))) badTextureRect.QueueFree();
        foreach (var badChild in GetChildren().Where(x => !x.IsAnyOf(_viewportsRoot, _textureRectRoot))) badChild.QueueFree();
    }

    private void ClearViewports() {
        foreach (var (name, (layer, root, viewport, rect, texture)) in _layers) {
            if (texture.IsValid()) texture.ViewportPath = new();
            if (root.IsValid()) root.Free();
            if (viewport.IsValid()) viewport.Free();
            if (rect.IsValid()) rect.Free();
        }
        _layers.Clear();
        if (_textureRectRoot.IsValid()) { _textureRectRoot.Free(); _textureRectRoot = null; }
        if (_viewportsRoot.IsValid()) { _viewportsRoot.Free(); _viewportsRoot = null; }
    }
}