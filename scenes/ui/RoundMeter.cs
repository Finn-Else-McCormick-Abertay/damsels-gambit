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

	public override void _EnterTree() { Update(); }
	public override void _ExitTree() { Clear(); }
	public override void _Ready() { Update(); }

	private void Update() {
		if (!IsInstanceValid(Container) || !IsInstanceValid(ProgressBar) || NodeScene is null) return;
		Clear();

		ProgressBar.Value = CurrentRound > NumRounds ? ProgressBar.MaxValue : 0f;

		for (int i = 1; i <= NumRounds; ++i) {
			var meterNode = NodeScene.Instantiate<Control>();
			Container.AddChild(meterNode); if (!Engine.IsEditorHint()) { meterNode.Owner = Container; }
			meterNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			meterNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			meterNode.CustomMinimumSize = new Vector2(50f, 50f);

			var activeVariant = meterNode.GetNode<Control>(i < CurrentRound ? "Completed" : i > CurrentRound ? "Upcoming" : "Current");
			activeVariant.Show();
			if (i == CurrentRound) {
				ProgressBar.Value = (1f - (ProgressBar.Size.X - Container.Size.X) / ProgressBar.Size.X) / NumRounds * i;

				var label = activeVariant.GetNode<Label>("Label");
				label.Text = $"{CurrentRound}";
			}
		}
	}

	private void Clear() {
		if (!IsInstanceValid(Container)) return;
		foreach (var child in Container.GetChildren()) {
			Container.RemoveChild(child); child.QueueFree();
		}
	}
}
