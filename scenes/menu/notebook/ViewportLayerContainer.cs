using System.Collections.Generic;
using System.Linq;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit;

[Tool, GlobalClass, Icon("res://assets/editor/icons/viewport_layer_container.svg")]
public partial class ViewportLayerContainer : Control, IReloadableToolScript
{
    [Export] private PackedScene[] Scenes { get; set { field = value; UpdateViewports(); } } = [];

	private Node _viewportsRoot; private Control _textureRectRoot;
    private readonly Dictionary<string, (Control Layer, Control Root, SubViewport Viewport, TextureRect TextureRect, ViewportTexture Texture)> _layers = [];

    public override void _ValidateProperty(Godot.Collections.Dictionary dict) {
        var property = dict["name"].AsStringName();
        if (property.IsAnyOf(PropertyName._viewportsRoot, PropertyName._textureRectRoot)) dict["usage"] = dict["usage"].AsInt64() | (long)PropertyUsageFlags.NoInstanceState;
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
        if (!IsInsideTree()) return;

        if (!_viewportsRoot.IsValid()) { _viewportsRoot = new() { Name = "ViewportsRoot" }; AddChild(_viewportsRoot); }
        if (!_textureRectRoot.IsValid()) { _textureRectRoot = new() { Name = "TextureRectRoot" }; AddChild(_textureRectRoot); }
        _textureRectRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        foreach (var (name, (layer, root, viewport, rect, texture)) in _layers) {
            if (!layer.IsValid() || !root.IsValid() || !viewport.IsValid() || !rect.IsValid() || !texture.IsValid()) {
                Console.Warning($"Invalid Layer '{name}'");
                if (root.IsValid()) root.QueueFree();
                if (viewport.IsValid()) viewport.QueueFree();
                if (rect.IsValid()) rect.QueueFree();
                _layers.Remove(name);
            }
        }

        foreach (var (index, layerScene) in Scenes.Index()) {
            if (!layerScene.IsValid()) continue;

            if (_layers.TryGetValue(layerScene.ResourcePath, out var tuple)) {
                if (tuple.Root.IsValid()) tuple.Root.Size = Size;
                if (tuple.Viewport.IsValid()) tuple.Viewport.Size = new Vector2I((int)Size.X, (int)Size.Y);
                if (tuple.TextureRect.IsValid()) tuple.TextureRect.ZIndex = index;
            }
            else {
		        var viewport2d = new SubViewport() { Msaa2D = Viewport.Msaa.Msaa8X, TransparentBg = true, Disable3D = true, Size = new Vector2I((int)Size.X, (int)Size.Y) }; _viewportsRoot.AddChild(viewport2d);

                var layerRoot = new Control() { Size = Size }; viewport2d.AddChild(layerRoot);
                var layer = layerScene.Instantiate() as Control; layerRoot.AddChild(layer); layer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

                //Engine.IsEditorHint() ? EditorInterface.Singleton.GetEditedSceneRoot().GetPathTo(viewport2d) : 
                var viewportTexture = new ViewportTexture() { ViewportPath = viewport2d.GetPath() };
                var textureRect = new TextureRect() { Texture = viewportTexture }; _textureRectRoot.AddChild(textureRect); textureRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                textureRect.ZIndex = index;

                _layers.Add(layerScene.ResourcePath, (layer, layerRoot, viewport2d, textureRect, viewportTexture));
            }
        }

        // Clean up instances of scenes which have been removed
        foreach (var (path, tuple) in _layers.Where(kvp => !Scenes.Where(x => x.IsValid()).Any(x => x.ResourcePath == kvp.Key))) {
            if (tuple.TextureRect.IsValid()) { _textureRectRoot.RemoveChild(tuple.TextureRect); tuple.Texture.ViewportPath = new(); tuple.TextureRect.QueueFree(); }
            if (tuple.Viewport.IsValid()) { _viewportsRoot.RemoveChild(tuple.Viewport); tuple.Viewport.QueueFree(); }
            _layers.Remove(path);
        }

        // Final sanity check (mainly clears up things left over by reloading after the script has changed)
        foreach (var badViewport in _viewportsRoot.GetChildren().Where(x => !x.IsAnyOf(_layers.Select(x => x.Value.Viewport)))) badViewport.QueueFree();
        foreach (var badTextureRect in _textureRectRoot.GetChildren().Where(x => !x.IsAnyOf(_layers.Select(x => x.Value.TextureRect)))) badTextureRect.QueueFree();
        foreach (var badChild in GetChildren().Where(x => !x.IsAnyOf(_viewportsRoot, _textureRectRoot))) badChild.QueueFree();

        /*Console.Info("Layers ", _layers.ToPrettyString());
        Console.Info("Viewports ", _viewportsRoot.GetChildren(true).ToPrettyString());
        Console.Info("TextureRects ", _textureRectRoot.GetChildren(true).ToPrettyString());
        Console.Info("Direct Children ", GetChildren(true).ToPrettyString());
        Console.Info(" --- ");*/
    }

    private void ClearViewports() {
        foreach (var (name, (layer, root, viewport, rect, texture)) in _layers) {
            if (texture.IsValid()) texture.ViewportPath = new();
            if (root.IsValid()) root.QueueFree();
            if (viewport.IsValid()) viewport.QueueFree();
            if (rect.IsValid()) rect.QueueFree();
        }
        _layers.Clear();
        if (_textureRectRoot.IsValid()) { RemoveChild(_textureRectRoot); _textureRectRoot.QueueFree(); }
        if (_viewportsRoot.IsValid()) { RemoveChild(_viewportsRoot); _viewportsRoot.QueueFree(); }
    }
}