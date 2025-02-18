using Godot;
using System;

public partial class Topic1 : Sprite2D
{
	private bool selected =false;
	private bool over = false;
	public string ID = "card1";
	// Called when the node enters the scene tree for the first time.
[Signal] public delegate void L1ADDEventHandler(string ID);
[Signal] public delegate void L1REMOVEEventHandler(string ID);

	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(Input.IsActionPressed("Left_Click") && over){
		selected = !selected;
		if(selected){
			EmitSignal(SignalName.L1ADD, ID);
		}else{
			EmitSignal(SignalName.L1REMOVE, ID);
		}}
		//if selected gray out
}	

	public void _mouseover() 
	{
		over = true;
	} 
	public void _mouseleave(){
		over = false;
	}
}
