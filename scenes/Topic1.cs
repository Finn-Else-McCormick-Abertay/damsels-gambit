using Godot;
using System;

public partial class Topic1 : Sprite2D
{
	private bool selected =false;
	public int ID = 1;
	public int deckspot;
	// Called when the node enters the scene tree for the first time.
[Signal] public delegate void L1ADDEventHandler(int ID);

	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//if selected gray out
	}

	public void select() 
	{
		if(Input.IsActionPressed("Left_Click")){
		selected = !selected;
		//if selected =true, add card id to decklist save deck spot
		//else remove from decklist and replace deckspot with another card
		}
	} 
}
