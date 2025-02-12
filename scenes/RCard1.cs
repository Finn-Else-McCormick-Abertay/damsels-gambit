using Godot;
using System;

public partial class RCard1 : Sprite2D
{
	private bool selected = false;
	private bool over = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	[Signal] public delegate void C4SELECTEventHandler();
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(over && Input.IsActionPressed("Left_Click")){
			selected = true;
		EmitSignal(SignalName.C4SELECT);
			selected=true;
			Position = new Vector2(
   			x: (50),
			y: (-30)); 
		
		}
		if(!selected && !over){
		Position = new Vector2(
   		x: (50),
		y: (70)); }
	}


	private void mouseover(){
		over=true;
		if(!selected){
		Position = new Vector2(
   		x: (50),
		y: (20)); 
		}

	}

	private void mouseleft(){
		over=false;

	}

	private void deselect(){
		selected = false;
	}
}
