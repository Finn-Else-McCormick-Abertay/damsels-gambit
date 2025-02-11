using Godot;
using System;


public partial class LeftDeck : Node2D
{
	// Called when the node enters the scene tree for the first time.

	private int[] Deck = {0,0,0,0,0,0};
	public override void _Ready()
	{
		Ongetdeck(01,02,03,04,05,06);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void Ongetdeck(int c1, int c2, int c3, int c4, int c5, int c6){
		Deck[0] = c1;
		Deck[1] = c2;
		Deck[2] = c3;
		Deck[3] = c4;
		Deck[4] = c5;
		Deck[5] = c6;
		shuffle(5);
	}
	private void OndrawSignal(int lastplay){
		if(lastplay !=3){
		int temp = Deck[lastplay];
		Deck[lastplay] = Deck[3]; 
		Deck[3] = temp;}

		shuffle(3);

		//signal draw deck[3];
	}

	private void shuffle(int size){
		Random rnd = new Random();
		int temp=0;
		for(int i=size; i>-1;i--){
			int num = rnd.Next(i);
			temp = Deck[num];
			Deck[num] = Deck[i];
			Deck[i] = temp;
		}
	}
}
