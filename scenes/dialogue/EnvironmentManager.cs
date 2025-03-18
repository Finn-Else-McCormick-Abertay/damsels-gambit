using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DamselsGambit.Util;
using Godot;
using YarnSpinnerGodot;

namespace DamselsGambit.Environment;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public partial class EnvironmentManager : Node
{
    public static EnvironmentManager Instance { get; private set; }
    
    private readonly Node _environmentRoot = new() { Name = "EnvironmentRoot" };
    private readonly Dictionary<string, Node> _environments = [];
    private readonly Dictionary<string, List<CharacterDisplay>> _characterDisplays = [];
    private readonly Dictionary<string, List<PropDisplay>> _propDisplays = [];

    public override void _EnterTree() {
        if (Instance is not null) throw AutoloadException.For(this);
        Instance = this; GetTree().Root.Connect(Node.SignalName.Ready, new Callable(this, MethodName.OnTreeReady), (uint)ConnectFlags.OneShot);
        this.AddOwnedChild(_environmentRoot);
    }
    private void OnTreeReady() => ReloadEnvironments(true);

    public void ReloadEnvironments(bool cleanupExisting = false) {
        // Clear any existing environments (animations can change the state of them, so we need to hard reset)
        _environments.Clear();
        foreach (var node in _environmentRoot.GetChildren()) { _environmentRoot.RemoveChild(node); node.QueueFree(); }

        foreach (var (fullPath, relativePath) in FileUtils.GetFilesOfTypeAbsoluteAndRelative<PackedScene>("res://scenes/environment/")) {
            var environmentName = relativePath.StripExtension();

            if (cleanupExisting) {
                var instances = GetTree().Root.FindChildrenWhere(x => x.SceneFilePath == fullPath);
                if (instances.Count > 1) foreach (var extraInstance in instances[1..]) extraInstance.QueueFree();

                var instance = instances.FirstOrDefault();
                if (instance is not null) {
                    _environments.Add(environmentName, instance);
                    Callable.From(() => {
                        instance.GetParent().RemoveChild(instance);
                        _environmentRoot.AddChild(instance); instance.Owner = _environmentRoot;
                        (instance as IEnvironmentDisplay)?.RestoreInitialVisibility();
                    }).CallDeferred();
                    continue;
                }
            }
            var scene = ResourceLoader.Load<PackedScene>(fullPath);
            if (scene is not null) {
                var node = scene.Instantiate();
                _environmentRoot.AddOwnedChild(node);
                _environments.Add(environmentName, node);
                foreach (var item in GetEnvironmentItems(environmentName)) item?.Set(CanvasItem.PropertyName.Visible, false);
            }
        }
    }

    public static void Register(CharacterDisplay display) => Instance?._characterDisplays?.GetOrAdd(display.CharacterName, [])?.Add(display);
    public static void Deregister(CharacterDisplay display) => Instance?._characterDisplays?.GetValueOrDefault(display.CharacterName)?.Remove(display);

    public static void Register(PropDisplay display) => Instance?._propDisplays?.GetOrAdd(display.PropName, [])?.Add(display);
    public static void Deregister(PropDisplay display) => Instance?._propDisplays?.GetValueOrDefault(display.PropName)?.Remove(display);

    public static IEnumerable<string> GetCharacterNames() => Instance?._characterDisplays?.Where(x => x.Value.Count > 0)?.Select(x => x.Key);
    public static IEnumerable<string> GetPropNames() => Instance?._propDisplays?.Where(x => x.Value.Count > 0)?.Select(x => x.Key);
    public static IEnumerable<string> GetEnvironmentNames() => Instance?._environments?.Keys;

    public static ReadOnlyCollection<CharacterDisplay> GetCharacterDisplays(string characterName) => Instance?._characterDisplays?.GetValueOr(characterName, [])?.AsReadOnly();
    public static ReadOnlyCollection<PropDisplay> GetPropDisplays(string propName) => Instance?._propDisplays?.GetValueOr(propName, [])?.AsReadOnly();

    // These are either CanvasLayers or CanvasItems - have to do it this way as they both have 'Visible' fields but are not derived from a shared interface
    public static IEnumerable<Node> GetEnvironmentItems(string environmentName) => Instance?._environments?.GetValueOrDefault(environmentName)?.GetSelfAndChildren()?.Where(node => node is CanvasLayer || node is CanvasItem) ?? [];
    public static IEnumerable<CanvasLayer> GetEnvironmentLayers(string environmentName) => Instance?._environments?.GetValueOrDefault(environmentName)?.GetSelfAndChildren()?.Where(node => node is CanvasLayer)?.Select(x => x as CanvasLayer) ?? [];

    public static IEnumerable<Node> GetAllItems(string itemName) => GetEnvironmentItems(itemName)?.Concat(GetCharacterDisplays(itemName))?.Concat(GetPropDisplays(itemName)) ?? [];
}