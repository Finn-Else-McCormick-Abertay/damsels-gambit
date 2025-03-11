using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

[Tool]
public partial class RoundMeter : Control, IReloadableToolScript
{
	[Export] public int CurrentRound { get; set { field = value; Update(); } } = 1;
	[Export] public int NumRounds { get; set { field = value; Update(); } } = 8;

	[ExportGroup("Nodes")]
	[Export] private ProgressBar ProgressBar { get; set; }
	[Export] private HBoxContainer Container { get; set; }
	[Export] private PackedScene NodeScene { get; set; }

	public override void _EnterTree() {
		ProgressBar?.TryConnect(Control.SignalName.Resized, new Callable(this, MethodName.Update));
		Container?.TryConnect(Control.SignalName.Resized, new Callable(this, MethodName.Update));
		CallableUtils.CallDeferred(() => this.OnReady(Update));
	}
	public override void _ExitTree() {
		Clear();
		ProgressBar?.TryDisconnect(Control.SignalName.Resized, new Callable(this, MethodName.Update));
		Container?.TryDisconnect(Control.SignalName.Resized, new Callable(this, MethodName.Update));
	}

	private void Update() {
		if (!IsInstanceValid(Container) || !IsInstanceValid(ProgressBar) || NodeScene is null) return;
		Clear();

		ProgressBar.Value = CurrentRound > NumRounds ? ProgressBar.MaxValue : 0f;

		for (int i = 1; i <= NumRounds; ++i) {
			var meterNode = NodeScene.Instantiate<Control>();
			Container.AddChild(meterNode);
			meterNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			meterNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			meterNode.CustomMinimumSize = new Vector2(50f, 50f);

			var activeVariant = meterNode.GetNode<Control>(i < CurrentRound ? "Completed" : i > CurrentRound ? "Upcoming" : "Current");
			activeVariant.Show();
			if (i == CurrentRound) {
				CallableUtils.CallDeferred(() => ProgressBar.Value = (meterNode.GlobalPosition.X + meterNode.Size.X / 2f - ProgressBar.GlobalPosition.X) / ProgressBar.Size.X * ProgressBar.MaxValue);
				activeVariant.GetNode<Label>("Label").Text = $"{CurrentRound}";
			}
		}
	}

	private void Clear() {
		if (!IsInstanceValid(Container)) return;
		foreach (var child in Container.GetChildren()) { Container.RemoveChild(child); child.QueueFree(); }
	}
}
