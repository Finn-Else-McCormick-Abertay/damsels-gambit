using System.Collections.Generic;
using YarnSpinnerGodot;
using Godot;
using System.Linq;
using System.Threading.Tasks;
using System;
using DamselsGambit.Util;
using DamselsGambit.Environment;
using System.Reflection;

namespace DamselsGambit.Dialogue;

static class AnimationDialogueCommands
{
    private static readonly Queue<Timer> Timers = [];

    private static void RunCommandDeferred(Action action) {
        //Console.Info($"Command Queued: {action.GetMethodInfo()}");
        var timer = Timers.LastOrDefault();
        if (timer is not null) {
            var awaiter = timer.ToSignal(timer, Timer.SignalName.Timeout);
            Task.Factory.StartNew(async () => {
                await awaiter;
                Callable.From(action).CallDeferred();
            });
        }
        else action?.Invoke();
    }

    // Non-blocking wait
    [YarnCommand("after")]
    public static void After(float time) {
        if (EnvironmentManager.Instance is null) return;
        var timer = new Timer() { OneShot = true, WaitTime = time };
        EnvironmentManager.Instance.AddChild(timer);
        Timers.Enqueue(timer);
        timer.Timeout += OnDeferralTimerTimeout;
        if (Timers.Count <= 1) timer.Start();
    }
    private static void OnDeferralTimerTimeout() {
        if (EnvironmentManager.Instance is null) return;
        if (Timers.TryDequeue(out var oldTimer)) {
            oldTimer.Timeout -= OnDeferralTimerTimeout;
            EnvironmentManager.Instance.RemoveChild(oldTimer);
            oldTimer.QueueFree();
        }
        if (Timers.TryPeek(out var nextTimer)) nextTimer.Start();
    }

    [YarnCommand("flush_command_queue")]
    public static void FlushCommandQueue() {
        foreach (var timer in Timers) {
            timer.Timeout -= OnDeferralTimerTimeout;
            timer.Connect(Timer.SignalName.Timeout, Callable.From(timer.QueueFree), (uint)GodotObject.ConnectFlags.OneShot);
            timer.Start(0.01f);
        }
        Timers.Clear();
    }

    [YarnCommand("scene")]
    public static void Scene(string sceneName, string action = "switch") => RunCommandDeferred(() => {
        if (action.MatchN("switch"))
            EnvironmentManager.GetEnvironmentNames()?.ForEach(name => EnvironmentManager.GetEnvironmentLayers(name)?.ForEach(x => x.Visible = !sceneName.MatchN("none") && name == sceneName));
        else if (action.MatchN("show") || action.MatchN("hide"))
            (sceneName.MatchN("all") ? EnvironmentManager.GetEnvironmentNames()?.SelectMany(EnvironmentManager.GetEnvironmentLayers) : EnvironmentManager.GetEnvironmentLayers(sceneName))?.ForEach(x => x.Visible = action.MatchN("show"));
    });

    [YarnCommand("show")]
    public static void Show(string itemName, int variant = -1) => RunCommandDeferred(() =>
        EnvironmentManager.GetAllItems(itemName)?.ForEach(x => {
                x?.Set(CanvasItem.PropertyName.Visible, true);
                if (variant >= 0 && x is PropDisplay propDisplay) propDisplay.Variant = variant;
            }));

    [YarnCommand("hide")]
    public static void Hide(string itemName) => RunCommandDeferred(() => EnvironmentManager.GetAllItems(itemName)?.ForEach(x => x?.Set(CanvasItem.PropertyName.Visible, false)));

    [YarnCommand("hide_box")]
    public static void HideBox() => RunCommandDeferred(() => {
        Console.Info("Dialogue Command 'hide_box' called");
        DialogueManager.DialogueViews?.ForEach(x => x.HideBox());
    });

    [YarnCommand("profile")]
    public static void Profile(string action) => RunCommandDeferred(() => {
        if (action.MatchN("open") || action.MatchN("close")) GameManager.NotebookMenu.Open = action.MatchN("open");
        if (action.MatchN("under") || action.MatchN("over")) GameManager.NotebookMenu.OverDialogue = action.MatchN("over");
        if (action.MatchN("show") || action.MatchN("hide")) GameManager.NotebookMenu.Visible = action.MatchN("show");
    });

    [YarnCommand("open_profile")]
    public static void OpenProfile() => RunCommandDeferred(() => GameManager.NotebookMenu.Open = true);

    [YarnCommand("close_profile")]
    public static void CloseProfile() => RunCommandDeferred(() => GameManager.NotebookMenu.Open = false);

    [YarnCommand("emote")]
    public static void Emote(string characterName, string emotionName, string from = "", string revertFrom = "") => RunCommandDeferred(() =>
        EnvironmentManager.GetCharacterDisplays(characterName).ForEach(display => {
            if (from == "from" && !string.IsNullOrWhiteSpace(revertFrom) && display.SpriteName != revertFrom) return;
            display.Show(); display.SpriteName = emotionName;
        }));

    [YarnCommand("move")]
    public static void Move(string itemName, float x, float y, float time = 0f) => RunCommandDeferred(()
        => EnvironmentManager.GetEnvironmentLayers(itemName)?.Select(x => x as Node)?.Concat(EnvironmentManager.GetCharacterDisplays(itemName))?.Concat(EnvironmentManager.GetPropDisplays(itemName))?.ForEach(item => {
            var property = item switch {
                Node2D => Node2D.PropertyName.Position,
                CanvasLayer => CanvasLayer.PropertyName.Offset,
                _ => throw new IndexOutOfRangeException()
            };
            if (time <= 0f) { item.Set(property, item.Get(property).AsVector2() + new Vector2(x, y)); return; }
            item.CreateTween().TweenProperty(item, property.ToString(), new Vector2(x, y), time).AsRelative();
        }));

    [YarnCommand("fade")]
    public static void Fade(string inOut, string itemName, float time) => RunCommandDeferred(() => {
        inOut = inOut.ToLower().Trim();
        if (inOut != "in" && inOut != "out") return;

        IEnumerable<CanvasItem> affectedItems = EnvironmentManager.GetCharacterDisplays(itemName).Cast<CanvasItem>().Concat(EnvironmentManager.GetPropDisplays(itemName).Cast<CanvasItem>());

        foreach (var layer in EnvironmentManager.GetEnvironmentLayers(itemName)) {
            if (time <= 0f) { if (inOut == "in") layer.Show(); else if (inOut == "out") layer.Hide(); continue; }
            var layerItems = layer.GetChildren().Cast<CanvasItem>().WhereExists().Where(x => !affectedItems.Contains(x));

            layer.Show();

            foreach (var item in layerItems) {
                if (!item.Visible) continue;

                float target = inOut switch { "in" => 1f, _ => 0f };
                item.Modulate = item.Modulate with { A = inOut switch { "in" => 0f, _ => item.Modulate.A } };
                
                var tween = item.CreateTween();
                tween.TweenProperty(item, "modulate:a", target, time);
            }

            if (inOut == "out") {
                var layerTween = layer.CreateTween();
                layerTween.TweenInterval(time);
                layerTween.TweenCallback(() => { layer.Hide(); foreach (var item in layerItems) item.Modulate = item.Modulate with { A = 1f }; });    
            }
        }

        if (time <= 0f) { foreach (var item in affectedItems) { if (inOut == "in") item.Show(); else if (inOut == "out") item.Hide(); } return; }

        foreach (var item in affectedItems) {
            float target = inOut switch { "in" => 1f, _ => 0f };
            item.Modulate = item.Modulate with { A = inOut switch { "in" => 0f, _ => item.Modulate.A } };
            
            item.Show();

            var tween = item.CreateTween();
            tween.TweenProperty(item, "modulate:a", target, time);
            if (inOut == "out") tween.TweenCallback(item.Hide);
        }
    });
}