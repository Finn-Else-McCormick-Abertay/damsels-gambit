using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;

namespace DamselsGambit;

[Tool]
public partial class RoundMeter : Control, IReloadableToolScript
{
	[Export] public int CurrentRound { get; set { field = value; this.OnReady(Update); } } = 1;
	[Export] public int NumRounds { get; set { field = value; this.OnReady(Update); } } = 8;
	
	[ExportGroup("Animation")]
	[Export] public double AnimationDuration { get; set; } = 0.0;
	[Export] public Tween.EaseType AnimationEase { get; set; } = Tween.EaseType.InOut;
	[Export] public Tween.TransitionType AnimationTransitionType { get; set; } = Tween.TransitionType.Linear;

	[ExportGroup("Nodes")]
	[Export] private ProgressBar ProgressBar { get; set; }
	[Export] private HBoxContainer Container { get; set; }
	[Export] private PackedScene NodeScene { get; set; }

	public override void _EnterTree() {
		ProgressBar?.TryConnect(Control.SignalName.Resized, this, MethodName.Update);
		Container?.TryConnect(Control.SignalName.Resized, this, MethodName.Update);
		this?.TryConnect(CanvasItem.SignalName.VisibilityChanged, this, MethodName.OnVisibilityChanged);
		this.OnReady(() => CallableUtils.CallDeferred(() => Update(true)));
		//if (Engine.IsEditorHint()) this.OnReady(() => CallableUtils.CallDeferred(() => Update(true))); else this.OnReady(() => Update(true));
	}
	public override void _ExitTree() {
		if (Container.IsValid()) foreach (var child in Container.GetChildren()) { Container.RemoveChild(child); child.QueueFree(); }
		foreach (var (_, tween) in _tweens) { if (IsInstanceValid(tween)) tween.Kill(); }
		_tweens.Clear();

		ProgressBar?.TryDisconnect(Control.SignalName.Resized, this, MethodName.Update);
		Container?.TryDisconnect(Control.SignalName.Resized, this, MethodName.Update);
		this?.TryDisconnect(CanvasItem.SignalName.VisibilityChanged, this, MethodName.OnVisibilityChanged);
	}

	private void OnVisibilityChanged() => Update(true);

	private readonly Dictionary<Node, Tween> _tweens = [];

	private void Update() => Update(false);
	private void Update(bool instant) {
		if (Container.IsInvalid() || ProgressBar.IsInvalid() || NodeScene is null) return;

		void AnimateNodeFade(Control meterNode, int index) {
			if (meterNode.IsInvalid()) return;
			Control completed = meterNode.GetNode<Control>("Completed"), current = meterNode.GetNode<Control>("Current"), upcoming = meterNode.GetNode<Control>("Upcoming");
			
			if (_tweens.TryGetValue(meterNode, out var oldTween) && IsInstanceValid(oldTween)) oldTween.Kill();

			if (AnimationDuration < 0 || instant) {
				completed.Visible = index < CurrentRound; completed.Modulate = completed.Modulate with { A = 1 };
				current.Visible = index == CurrentRound; current.Modulate = current.Modulate with { A = 1 };
				upcoming.Visible = index >= CurrentRound; upcoming.Modulate = upcoming.Modulate with { A = 1 };
				return;
			}

			if (index < CurrentRound) {
				completed.Show();
				if (current.Visible) {
					upcoming.Show(); upcoming.Modulate = upcoming.Modulate with { A = 1 };
					_tweens[meterNode] = CreateTween();
					_tweens[meterNode].TweenProperty(upcoming, "modulate:a", 0, AnimationDuration);
					_tweens[meterNode].TweenProperty(upcoming, "visible", false, 0);
				}
				else { upcoming.Hide(); }
				current.Hide();
			}
			else if (index > CurrentRound) {
				upcoming.Show();
				if (current.Visible) { completed.Show(); upcoming.Modulate = upcoming.Modulate with { A = 0 }; }
				_tweens[meterNode] = CreateTween();
				_tweens[meterNode].TweenProperty(upcoming, "modulate:a", 1, AnimationDuration);
				current.Hide();
			}
			else { current.Show(); }
		}

		void AnimateProgressBarTo(Control node) {
			if (node is not null && node.IsInvalid()) return;
			double val = node switch { Control => (node.GlobalPosition.X + node.Size.X / 2f - ProgressBar.GlobalPosition.X) / ProgressBar.Size.X * ProgressBar.MaxValue, _ => CurrentRound > NumRounds ? ProgressBar.MaxValue : 0f };

			if (node is not null) { ProgressBar.Value += node.GetNode<Control>("Completed").Size.X / 8 * 3 / ProgressBar.Size.X * ProgressBar.MaxValue * (val < ProgressBar.Value ? -1 : 1); }

			if (_tweens.TryGetValue(ProgressBar, out var oldTween) && IsInstanceValid(oldTween)) oldTween.Kill();
			if (AnimationDuration <= 0 || instant) { ProgressBar.Value = val; return; }
			_tweens[ProgressBar] = CreateTween(); _tweens[ProgressBar].TweenProperty(ProgressBar, ProgressBar.PropertyName.Value, val, AnimationDuration).SetEase(AnimationEase).SetTrans(AnimationTransitionType);
		}

		foreach (var child in Container.GetChildren()) { if (child.GetIndex() >= NumRounds) { Container.RemoveChild(child); child.QueueFree(); } }
		foreach (int i in RangeOf<int>.UpTo(int.Max(0, NumRounds - Container.GetChildCount()))) {
			var meterNode = NodeScene.Instantiate<Control>(); Container.AddChild(meterNode);
			Control completed = meterNode.GetNode<Control>("Completed"), current = meterNode.GetNode<Control>("Current"), upcoming = meterNode.GetNode<Control>("Upcoming");
			completed.Visible = i < CurrentRound;
			current.Visible = i == CurrentRound;
			upcoming.Visible = i >= CurrentRound; upcoming.Modulate = upcoming.Modulate with { A = 1 };
		}
		
		if (CurrentRound < 1 || CurrentRound > NumRounds) AnimateProgressBarTo(null);
		foreach (int i in RangeOf<int>.Over(1, NumRounds)) {
			var meterNode = Container.GetChild<Control>(i - 1);
			if (meterNode.FindChildOfType<Label>() is Label label) label.Text = $"{i}";
			AnimateNodeFade(meterNode, i);
		}
		Container.QueueSort();
		Container.Connect(HBoxContainer.SignalName.SortChildren, () => {
			AnimateProgressBarTo(CurrentRound switch { >= 1 => Container.GetChildOrNull<Control>(CurrentRound - 1 ), _ => null });
		}, (uint)ConnectFlags.OneShot);
	}
}
