using Godot;
using System;

public partial class TopicDecklist : Label
{
	// Called when the node enters the scene tree for the first time.
	private int[] decklist = {0,0,0,0,0,0};
	private int cardplace = 0;

	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void deckadd(int id) {
		decklist[cardplace] = id;
		 cardplace++;
	}
}
