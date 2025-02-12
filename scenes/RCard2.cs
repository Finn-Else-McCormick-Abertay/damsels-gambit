using Godot;
using System;

public partial class RCard2 : Sprite2D
{
	[Signal] public delegate void C5SELECTEventHandler();

	private bool selected = false;
	private bool over = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(over && Input.IsActionPressed("Left_Click")){
			selected = true;
			EmitSignal(SignalName.C5SELECT);
			selected=true;
			Position = new Vector2(
   			x: (50),
			y: (-30)); 
		
		}
		if(!over && !selected){
		Position = new Vector2(
   		x: (50),
		y: (70));
		}
	}


	private void mouseover2(){
		over=true;
		if(!selected){
		Position = new Vector2(
   		x: (50),
		y: (20)); 
		}

	}

	private void mouseleft2(){
		over=false;

	}

	private void deselect(){
		selected = false;
	}	
}
