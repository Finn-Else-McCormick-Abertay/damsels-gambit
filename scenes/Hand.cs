using Godot;
using System;

public partial class Hand : Node2D
{
	// Called when the node enters the scene tree for the first time.
	[Signal]
	public delegate void C1SelectEventHandler();
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var card1 = GetNode<LCard1>("./LCard1");
		var card2 = GetNode<LCard1>("./LCard2");
		var card3 = GetNode<LCard3>("./LCard3");

		card1.select = true;
		card1.move(new Vector2(x: (550), y: (550)));

		Vector2 mousepos = GetViewport().GetMousePosition();
		if (mousepos.X > 380 && mousepos.X < 480 && mousepos.Y > 480 && Input.IsActionPressed("Left_Click")){
		card1.deselect();
		card2.deselect();

	}		
	if (mousepos.X > 260 && mousepos.X < 360 && mousepos.Y > 480 && Input.IsActionPressed("Left_Click")){
		card1.deselect();
		card3.deselect();
	}
	if (mousepos.X > 140 && mousepos.X < 240 && mousepos.Y > 480 && Input.IsActionPressed("Left_Click")){
		EmitSignal(SignalName.C1Select);
	}
}
}
