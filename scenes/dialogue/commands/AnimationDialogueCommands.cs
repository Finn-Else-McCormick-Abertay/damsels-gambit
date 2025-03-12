using System.Collections.Generic;
using YarnSpinnerGodot;
using Godot;
using System.Linq;
using System.Threading.Tasks;
using System;
using Eltons.ReflectionKit;
using DamselsGambit.Util;

namespace DamselsGambit.Dialogue;

static class AnimationDialogueCommands
{
    private static readonly Queue<Timer> Timers = new();

    private static void RunCommandDeferred(Action action) {
        var actionSignature = action.Method.GetSignature(false);
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
        if (DialogueManager.Instance is null) return;
        var timer = new Timer() { OneShot = true, WaitTime = time };
        DialogueManager.Instance.AddChild(timer);
        Timers.Enqueue(timer);
        timer.Timeout += OnDeferralTimerTimeout;
        if (Timers.Count <= 1) timer.Start();
    }
    private static void OnDeferralTimerTimeout() {
        if (DialogueManager.Instance is null) return;
        if (Timers.TryDequeue(out var oldTimer)) {
            oldTimer.Timeout -= OnDeferralTimerTimeout;
            DialogueManager.Instance.RemoveChild(oldTimer);
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
    public static void Scene(string sceneName) => RunCommandDeferred(()
        => DialogueManager.GetEnvironmentNames()?.ForEach(name => DialogueManager.GetEnvironmentItems(name)?.ForEach(x => x?.Set(CanvasItem.PropertyName.Visible, name == sceneName))));

    [YarnCommand("show")]
    public static void Show(string itemName) => RunCommandDeferred(() => DialogueManager.GetAllItems(itemName)?.ForEach(x => x?.Set(CanvasItem.PropertyName.Visible, true)));

    [YarnCommand("hide")]
    public static void Hide(string itemName) => RunCommandDeferred(() => DialogueManager.GetAllItems(itemName)?.ForEach(x => x?.Set(CanvasItem.PropertyName.Visible, false)));

    [YarnCommand("emote")]
    public static void Emote(string characterName, string emotionName, string from = "", string revertFrom = "") {
        RunCommandDeferred(() =>
            DialogueManager.GetCharacterDisplays(characterName).ForEach(display => {
                if (from == "from" && !string.IsNullOrWhiteSpace(revertFrom) && display.SpriteName != revertFrom) return;
                display.Show(); display.SpriteName = emotionName;
            })
        );
    }

    [YarnCommand("move")]
    public static void Move(string itemName, float x, float y, float time = 0f) => RunCommandDeferred(()
        => DialogueManager.GetAllItems(itemName)?.ForEach(item => {
            var property = item switch {
                Node2D => Node2D.PropertyName.Position,
                CanvasLayer => CanvasLayer.PropertyName.Offset,
                _ => throw new IndexOutOfRangeException()
            };
            if (time <= 0f) { item.Set(property, item.Get(property).AsVector2() + new Vector2(x, y)); return; }
            item.CreateTween().TweenProperty(item, property.ToString(), new Vector2(x, y), time).AsRelative();
        }));

    [YarnCommand("fade")]
    public static void Fade(string inOut, string itemName, float time) {
        RunCommandDeferred(() => {
            List<CanvasItem> affectedItems = [];
            List<CanvasLayer> affectedLayers = [];

            var environmentItems = DialogueManager.GetAllItems(itemName);

            affectedItems.AddRange(environmentItems.Where(x => x is CanvasItem).Select(x => x as CanvasItem));
            foreach (var layer in environmentItems.Where(x => x is CanvasLayer).Select(x => x as CanvasLayer)) {
                var affectedInLayer = layer.GetChildren().Where(x => x is CanvasItem).Select(x => x as CanvasItem);
                affectedItems.AddRange(affectedInLayer);
                if (affectedInLayer.Any()) affectedLayers.Add(layer);
            }

            if (time <= 0f) {
                foreach (var item in affectedItems) if (inOut == "in") item.Show(); else if (inOut == "out") item.Hide();
                foreach (var layer in affectedLayers) if (inOut == "in") layer.Show(); else if (inOut == "out") layer.Hide();
                return;
            }

            foreach (var item in affectedItems) {
                float target;
                if (inOut == "in") { item.Modulate = item.Modulate with { A = 0f }; target = 1f; } else if (inOut == "out") { item.Modulate = item.Modulate with { A = 1f }; target = 0f; }
                else continue;
                
                item.Show();

                var tween = item.CreateTween();
                tween.TweenProperty(item, "modulate:a", target, time);

                if (inOut == "out") { tween.TweenCallback(Callable.From(item.Hide)); }
            }
            
            foreach (var layer in affectedLayers) {
                layer.Show();
                if (inOut == "out") {
                    var tween = layer.CreateTween();
                    tween.TweenInterval(time);
                    tween.TweenCallback(Callable.From(layer.Hide));
                }
            }
        });
    }
}