using Godot;
using System;

public partial class LCard1 : Sprite2D
{
	// Called when the node enters the scene tree for the first time.
	[Export]
	public bool select { get; set; } = false;
	public override void _Ready()
	{
		select = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	bool selected = false;
	public override void _Process(double delta)
	{
		
		Vector2 mousepos = GetViewport().GetMousePosition();
		if(!selected){
		if (mousepos.X > 140 && mousepos.X < 240 && mousepos.Y > 480){
			Position = new Vector2(
   		 	x: (190),
			y: (550)); 
		}else{
			Position = new Vector2(
   		 	x: (190),
			y: (600)); 
		}}
		if (mousepos.X > 140 && mousepos.X < 240 && mousepos.Y > 480 && Input.IsActionPressed("Left_Click")|| selected){

			selected = true;
		}
		if(selected){
			Position = new Vector2(
   		 	x: (450),
			y: (400)); 
		}
		if (mousepos.X > 400 && mousepos.X < 500 && mousepos.Y > 330 && mousepos.Y < 470 && Input.IsActionPressed("Left_Click") && selected){
			Position = new Vector2(
   		 	x: (430),
			y: (600)); 
			deselect();
		}


	}
		public void deselect(){
			selected = false;
			select = false;
		}

		public void move(Vector2 place){
			Position = place;
		}

}
