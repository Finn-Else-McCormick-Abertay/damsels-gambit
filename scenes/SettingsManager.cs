using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public sealed partial class SettingsManager : Node
{
	public static SettingsManager Instance { get; private set; }
    public override void _EnterTree() {
        if (Instance is not null) throw AutoloadException.For(this); Instance = this;
        if (!Load()) { CreateDefaultConfig(); Save(); }
    }

    private static readonly string ConfigPath = "user://settings.cfg";
    private static Dictionary<string, Dictionary<string, Variant>> _config = [];

    public static bool Load() {
        var file = new ConfigFile();
        if (file.Load(ConfigPath) != Error.Ok) return false;
        _config = file.GetSections().Select(section =>
            new { Key = section, Value = file.GetSectionKeys(section)
                .Select(key => new { Key = key, Value = file.GetValue(section, key)}).ToDictionary(pair => pair.Key, pair => pair.Value)
            }).ToDictionary(pair => pair.Key, pair => pair.Value);
        return true;
    }

    public static void Save() {
        var file = new ConfigFile();

        foreach (var (section, key, value) in
                from section in _config.Keys from pair in _config[section] select (section, pair.Key, pair.Value))
            file.SetValue(section, key, value);
        
        file.Save(ConfigPath);
    }

    private static void CreateDefaultConfig() {
        _config.Clear();

        SetConfig("graphics", "fullscreen", false);
        SetConfig("graphics", "resolution", new());

        IEnumerable<string> audioBusNames = [ "Master", "Music", "SFX" ];
        foreach (var busName in audioBusNames) SetConfig("audio", $"{Case.ToSnake(busName)}_volume", AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex(busName)));

        SetConfig("accessibility", "font", Enum.GetName(FontManager.FontState.Default));
    }

    public static Variant GetConfig(string section, string key) => _config.TryGetValue(section, out var sectionDict) ? sectionDict.TryGetValue(key, out var val) ? val : new() : new();
    public static T GetConfig<[MustBeVariant]T>(string section, string key) => GetConfig(section, key).As<T>();

    public static void SetConfig(string section, string key, Variant value) => _config.GetOrAdd(section, [])[key] = value;
    public static void ClearConfig(string section, string key) { if (_config.TryGetValue(section, out var dict)) dict.Remove(key); }
}