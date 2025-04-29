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
    private static readonly Dictionary<string, Dictionary<string, Variant>> _config = [];
    private static readonly Dictionary<(string, string), Action<Variant>> _specificConfigHandlers = [];
    private static readonly Dictionary<string, Action<string, Variant>> _parametricConfigHandlers = [];

    static SettingsManager() {
        SetConfigHandler("audio", (string key, float volume) => {
            Console.Info($"audio/{key} set to {volume}");
            int busIndex = AudioServer.GetBusIndex(key.Trim().StripBack(" volume").Trim());
            AudioServer.SetBusVolumeLinear(busIndex, volume);
        });
    }

    public static bool Load() {
        Console.Info($"Loading config from {ConfigPath}");
        var file = new ConfigFile();
        if (file.Load(ConfigPath) != Error.Ok) return false;
        var newConfig = file.GetSections().Select(section =>
            new { Key = section, Value = file.GetSectionKeys(section)
                .Select(key => new { Key = key, Value = file.GetValue(section, key)}).ToDictionary(pair => pair.Key, pair => pair.Value)
            }).ToDictionary(pair => pair.Key, pair => pair.Value);

        ClearAllConfigs();
        foreach (var (section, dict) in newConfig) foreach (var (key, value) in dict) SetConfig(section, key, value);
        return true;
    }

    public static void Save() {
        Console.Info($"Saving config to {ConfigPath}");
        var file = new ConfigFile();
        foreach (var (section, key, value) in from section in _config.Keys from pair in _config[section] select (section, pair.Key, pair.Value)) file.SetValue(section, key, value);
        file.Save(ConfigPath);
    }

    private static void CreateDefaultConfig() {
        Console.Info("Creating default config");
        ClearAllConfigs();

        SetConfig("graphics", "fullscreen", false);
        SetConfig("graphics", "resolution", new());

        IEnumerable<string> audioBusNames = [ "Master", "Music", "SFX" ];
        foreach (var busName in audioBusNames) SetConfig("audio", $"{busName} volume", AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex(busName)));

        SetConfig("accessibility", "font", Enum.GetName(FontManager.FontState.Default));
    }

    public static Variant GetConfig(string section, string key) => _config.TryGetValue(section, out var sectionDict) ? sectionDict.TryGetValue(key, out var val) ? val : new() : new();
    public static T GetConfig<[MustBeVariant]T>(string section, string key) => GetConfig(section, key).As<T>();

    public static void SetConfig(string section, string key, Variant value) {
        _config.GetOrAdd(section, [])[key] = value;
        if (_specificConfigHandlers.TryGetValue((section, key), out var specificHandler) && specificHandler is not null) specificHandler?.Invoke(value);
        if (_parametricConfigHandlers.TryGetValue(section, out var parametricHandler) && parametricHandler is not null) parametricHandler?.Invoke(key, value);
    }

    public static void ClearConfig(string section, string key) { if (_config.TryGetValue(section, out var dict)) dict.Remove(key); }
    public static void ClearAllConfigs() => _config.Clear();

    // Try to call from a static constructor or something so it can be registered before SettingsManager is initialised.
    public static void SetConfigHandler(string section, string key, Action<Variant> handler) => _specificConfigHandlers[(section, key)] = handler;
    public static void SetConfigHandler(string section, Action<string, Variant> handler) => _parametricConfigHandlers[section] = handler;

    public static void SetConfigHandler<[MustBeVariant]T>(string section, string key, Action<T> handler) => SetConfigHandler(section, key, variant => handler(variant.As<T>()));
    public static void SetConfigHandler<[MustBeVariant]T>(string section, Action<string, T> handler) => SetConfigHandler(section, (key, variant) => handler(key, variant.As<T>()));

    public static void ClearConfigHandler(string section, string key) => _specificConfigHandlers.Remove((section, key));
    public static void ClearConfigHandler(string section) => _parametricConfigHandlers.Remove(section);
    public static void ClearAllConfigHandlers() { _specificConfigHandlers.Clear(); _parametricConfigHandlers.Clear(); }
}