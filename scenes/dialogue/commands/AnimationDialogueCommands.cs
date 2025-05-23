using System.Collections.Generic;
using YarnSpinnerGodot;
using Godot;
using System.Linq;
using System.Threading.Tasks;
using System;
using DamselsGambit.Util;
using DamselsGambit.Environment;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;

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

    private static string GetPrintoutPrefix(string methodName, string methodInfo) => $"Dialogue Command [{Case.ToSnake(methodName)}{methodInfo switch { string => $" {methodInfo.StripFront('(').StripBack(')')}", null => "" }}]";

    private static void CommandInfo(string msg, string extraMethodInfo = null, [CallerMemberName]string callingMethod = null) => Console.Info($"{GetPrintoutPrefix(callingMethod, extraMethodInfo)}: {msg}", false);
    private static void CommandWarning(string msg, string extraMethodInfo = null, [CallerMemberName]string callingMethod = null) => Console.Warning($"{GetPrintoutPrefix(callingMethod, extraMethodInfo)}: {msg}", false);
    private static void CommandError(string msg, string extraMethodInfo = null, [CallerMemberName]string callingMethod = null) => Console.Error($"{GetPrintoutPrefix(callingMethod, extraMethodInfo)}: {msg}", false);

    // Non-blocking wait
    [YarnCommand("after")]
    public static void After(float time) {
        if (EnvironmentManager.Instance is null) return;
        if (time < 0) { CommandWarning($"Time cannot be less than zero.", $"{time}"); }
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
        action = action.Trim().ToLower();
        if (action == "switch")
            EnvironmentManager.GetEnvironmentNames()?.ForEach(name => EnvironmentManager.GetEnvironmentLayers(name)?.ForEach(x => x.Visible = !sceneName.MatchN("none") && name == sceneName));
        else if (action.IsAnyOf("show", "hide"))
            (sceneName.MatchN("all") ? EnvironmentManager.GetEnvironmentNames()?.SelectMany(EnvironmentManager.GetEnvironmentLayers) : EnvironmentManager.GetEnvironmentLayers(sceneName))?.ForEach(x => x.Visible = action == "show");
        else
            CommandError($"Invalid action arg '{action}': should be one of 'switch', 'show' or 'hide'.", $"{sceneName} {action}");
    });

    [YarnCommand("variant")]
    public static void Variant(string itemName, string variant) => RunCommandDeferred(() =>
        EnvironmentManager.GetPropDisplays(itemName)?.ForEach(x => {
            int newVariant = 0;
            if (int.TryParse(variant, out int variantNum)) newVariant = variantNum;
            else if (variant.ToLower().Trim() == "default") newVariant = PropDisplay.GetValidVariants(itemName).FirstOrDefault();
            else if (variant.ToLower().Trim().StartsWith("random")) {
                var randomArgs = variant.ToLower().Trim().StripFront("random");
                var validVariants = PropDisplay.GetValidVariants(itemName).ToList();

                if (randomArgs.StartsWith('[') && randomArgs.EndsWith(']')) {
                    randomArgs = randomArgs.StripFront('[').StripBack(']');
                    if (randomArgs.IsNullOrWhitespace()) validVariants = [];
                    else if (!randomArgs.Equals("all", StringComparison.CurrentCultureIgnoreCase)) {
                        var splitArgs = randomArgs.Split(',', StringSplitOptions.TrimEntries);
                        try {
                            IEnumerable<int> indexArgs = splitArgs.Index().Select(x => {
                                var arg = x.Item.Trim();
                                if (arg.Split("..") is string[] rangePoints && rangePoints.Length >= 2) {
                                    if (rangePoints[0].IsNullOrWhitespace()) rangePoints[0] = validVariants.FirstOrDefault().ToString();
                                    if (rangePoints[^1].IsNullOrWhitespace()) rangePoints[^1] = validVariants.LastOrDefault().ToString();
                                    var rangeValues = rangePoints.Select(int.Parse).ToList();
                                    List<int> fullRange = [];
                                    bool? goingForwards = null;
                                    foreach (var (index, value) in rangeValues.Index()) {
                                        if (index == 0) continue;
                                        bool forwards = value > rangeValues[index - 1];
                                        if (goingForwards is not null && goingForwards != forwards) throw new FormatException($"({x.Index}) Range expression switched direction.");
                                        goingForwards = forwards;
                                        for (int i = rangeValues[index - 1]; forwards ? i < value : i > value; i = forwards ? ++i : --i) fullRange.Add(i);
                                    }
                                    fullRange.Add(rangeValues.Last());
                                    return fullRange.AsEnumerable();
                                }
                                else return [ int.Parse(arg) ];
                            }).SelectMany(x => x);
                            if (indexArgs.Any(x => !validVariants.Contains(x))) CommandWarning($"Nonexistent variants passed as random args. Valid variants: [{string.Join(", ", validVariants)}]", $"{itemName} {variant}");
                            validVariants = [..validVariants.Where(x => indexArgs.Contains(x))];
                        }
                        catch (FormatException fmtException) { CommandError($"Format exception while parsing random args: {fmtException.Message}", $"{itemName} {variant}"); }
                    }
                    else CommandError($"Invalid random args '{randomArgs}'.", $"({itemName} {variant})");
                }
                else if (!randomArgs.IsNullOrWhitespace()) CommandError($"Invalid variant arg. Must be an integer, 'default', 'random' or 'random[{{range expression}}]'.", $"({itemName} {variant})");

                newVariant = validVariants.OrderBy(x => Random.Shared.Next()).FirstOrDefault();
                //CommandInfo($"Selecting variant {newVariant} at random from [{string.Join(", ", validVariants)}].", $"{itemName} {variant}");
            }
            else if (!variant.IsNullOrWhitespace()) { CommandError($"Invalid variant arg. Must be an integer, 'default', 'random' or 'random[{{range expression}}]'.", $"{itemName} {variant}"); }

            if (!PropDisplay.GetValidVariants(itemName).Contains(newVariant)) CommandError($"No such variant '{newVariant}'.", $"{itemName} {variant}");
            x.Variant = newVariant;
        }));

    private static void PropAnimate(string itemName, string type = "") {
        type = type.Trim().ToLower();
        float moveSpeed = 1f, waitTime = 2f; 
        var players = EnvironmentManager.GetAllAnimationPlayers();

        bool PlayAnimation(string name, string fade) {
            var validPlayers = players.Where(x => x.HasAnimation(name));
            if (!validPlayers.Any()) return false;
            float timeToBlock = 0f;
            foreach (var player in validPlayers) { player.Play(name); player.Advance(0); timeToBlock = MathF.Max(timeToBlock, (float)player.CurrentAnimationLength); }
            Fade(fade, itemName, timeToBlock); After(timeToBlock);
            return true;
        }

        if (type.IsAnyOf("in", "enter", "full", "inout", "")) {
            if (!PlayAnimation("item_enter", "in")) {
                Move(itemName, 0f, 200f, 0);
                Move(itemName, 0f, -200f, moveSpeed);
                Fade("in", itemName, moveSpeed);
                After(moveSpeed);
            }
        }
        if (type.IsAnyOf("full", "inout", "")) After(waitTime);
        if (type.IsAnyOf("out", "exit", "full", "inout", "")) {
            if (!PlayAnimation("item_exit", "out")){
                Move(itemName, 0f, -200f, moveSpeed);
                Fade("out", itemName, moveSpeed); After(moveSpeed);
                Move(itemName, 0f, 200f, 0);
            }
        }
    }
    
    [YarnCommand("prop")]
    public static void Prop(string action, string itemName, string actionArg1 = "", string actionArg2 = "") {
        switch (action.Trim().ToLower()) {
            case "variant": Variant(itemName, actionArg1); break;
            case "animate": {
                bool firstArgIsAnimateArg = actionArg1.Trim().ToLower().IsAnyOf("in", "out", "enter", "exit", "full", "inout");
                Variant(itemName, !firstArgIsAnimateArg ? actionArg1 : actionArg2);
                PropAnimate(itemName, firstArgIsAnimateArg ? actionArg1 : actionArg2);
            } break;
            default: CommandError($"Invalid arg '{action}': must be 'variant' or 'animate'.", $"{action} {itemName}{(actionArg1.IsEmpty() ? "" : $" {actionArg1}")}{(actionArg2.IsEmpty() ? "" : $" {actionArg2}")}"); break;
        }
    }

    [YarnCommand("show")]
    public static void Show(string itemName, string variant = "") => RunCommandDeferred(itemName.Trim().ToLower() switch {
        "profile" => () => GameManager.NotebookMenu.Visible = true,
        _ => () => EnvironmentManager.GetAllItems(itemName)?.ForEach(x => { x?.Set(CanvasItem.PropertyName.Visible, true); if (!variant.IsNullOrWhitespace()) Variant(itemName, variant); })
    });

    [YarnCommand("hide")]
    public static void Hide(string itemName) => RunCommandDeferred(itemName.Trim().ToLower() switch {
        "profile" => () => GameManager.NotebookMenu.Visible = false,
        "box" => () => DialogueManager.DialogueViews?.ForEach(x => x.HideBox()),
        _ => () => EnvironmentManager.GetAllItems(itemName)?.ForEach(x => x?.Set(CanvasItem.PropertyName.Visible, false))
    });

    [YarnCommand("profile")]
    public static void Profile(string action) => RunCommandDeferred(() => {
        action = action.Trim().ToLower();
        bool? result = action switch {
            "open" or "close" => GameManager.NotebookMenu.Open = action == "open",
            "under" or "over" => GameManager.NotebookMenu.OverDialogue = action == "over",
            "show" or "hide" => GameManager.NotebookMenu.Visible = action == "show",
            _ => null
        };
        if (result is null) CommandError($"Invalid arg '{action}': should be one of 'open', 'close', 'over', 'under', 'show' or 'hide'.", $"{action}");
    });

    [YarnCommand("emote")]
    public static void Emote(string characterName, string emotionName, bool from = false, string revertFrom = "") => RunCommandDeferred(() => {
        if (from && revertFrom.IsNullOrWhitespace()) CommandWarning("Using arg 'from', but no emotion to revert from specified.", $"{characterName} {emotionName}{from switch { true => " from ", false => "" }}{revertFrom}");
        EnvironmentManager.GetCharacterDisplays(characterName).ForEach(display => {
            if (from && !string.IsNullOrWhiteSpace(revertFrom) && display.SpriteName != revertFrom) return;
            display.Show(); display.SpriteName = emotionName;
            if (!display.SpriteExists) Console.Warning($"{characterName} does not have an emotion called {emotionName}.", $"{characterName} {emotionName}{from switch { true => " from ", false => "" }}{revertFrom}");
        });
    });
    
    [YarnCommand("animation")]
    public static void Animation(string animationName, bool block = false) => RunCommandDeferred(() => {
        var validPlayers = EnvironmentManager.GetAllAnimationPlayers().Where(x => x.HasAnimation(animationName));
        if (!validPlayers.Any()) Console.Warning("Animation could not be found", $"{animationName}{block switch { true => " block", false => "" }}");
        float timeToBlock = 0f;
        foreach (var animationPlayer in validPlayers) {
            animationPlayer.Play(animationName); animationPlayer.Advance(0);
            timeToBlock = MathF.Max(timeToBlock, (float)animationPlayer.CurrentAnimationLength);
        }
        if (block && timeToBlock > 0f) After(timeToBlock);
    });

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
        if (inOut != "in" && inOut != "out") { CommandError($"Invalid arg '{inOut}': must be 'in' or 'out'.", $"{inOut} {itemName} {time}"); return; }

        IEnumerable<CanvasItem> affectedItems = EnvironmentManager.GetCharacterDisplays(itemName).Cast<CanvasItem>().Concat(EnvironmentManager.GetPropDisplays(itemName).Cast<CanvasItem>());

        foreach (var layer in EnvironmentManager.GetEnvironmentLayers(itemName)) {
            if (time <= 0f) { if (inOut == "in") layer.Show(); else if (inOut == "out") layer.Hide(); continue; }
            var layerItems = layer.GetChildren().Where(x => x is CanvasItem).Cast<CanvasItem>().Where(x => !affectedItems.Contains(x));

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
            if (inOut == "out" && item is PropDisplay) tween.TweenCallback(item.Hide);
        }
    });
}