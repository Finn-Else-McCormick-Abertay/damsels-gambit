using Godot;
using System;

public partial class TopicDecklist : Label
{
	// Called when the node enters the scene tree for the first time.
	private string[] decklist = {" 1"," 2"," 3"," 4"," 5"," 6"};
	private int cardplace = 0;

	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		string full =" ";

		for(int i =0; i <6; i++){
			full = full + decklist[i];
		}
		
		Text = full;

	}

	
	private void deckadd(string id) {
		if(cardplace > 5){
			cardplace = 5;
		}
		decklist[cardplace] = id;
		 cardplace++;
	}

	private void deckremove(string id){
		bool found= false;
		for(int i =0; i <cardplace; i++){
			if(found){
				decklist[i-1]= decklist[i];
			}
			if(decklist[i] == id){
				found = true;
			}
		}
		if(found){
			decklist[cardplace] = " ";
			cardplace--;
		}
	}
}
