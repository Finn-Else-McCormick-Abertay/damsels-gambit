using Godot;
using System;

public partial class RCard3 : Sprite2D
{
		[Signal] public delegate void C6SELECTEventHandler();
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
			EmitSignal(SignalName.C6SELECT);
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


	private void mouseover3(){
		over=true;
		if(!selected){
		Position = new Vector2(
   		x: (50),
		y: (20)); 
		}
	}

	private void mouseleft3(){
		over=false;

	}

	private void deselect(){
		selected = false;
	}	
}
