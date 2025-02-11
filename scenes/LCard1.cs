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
	[Signal] public delegate void C1SELECTEventHandler();
	private bool selected = false;
	public override void _Process(double delta)
	{
		
		Vector2 mousepos = GetViewport().GetMousePosition();
		if(!selected){
		if(mouseover()){
			Position = new Vector2(
   		 	x: (190),
			y: (550)); 
			if(Input.IsActionPressed("Left_Click")){
				selected = true;
				EmitSignal(SignalName.C1SELECT);
			}
		}else{
			Position = new Vector2(
   		 	x: (190),
			y: (600)); 
		}}else{
			Position = new Vector2(
   		 	x: (190),
			y: (500)); 
		}


	}
	private void OnC2SELECTSignal(){
		selected =false;
	}
	private void _on_l_card_3_c_3select(){
		selected =false;
	}

		private bool mouseover(){

		Vector2 mousepos = GetViewport().GetMousePosition();

		if(mousepos.X > Position.X - 50 && mousepos.X < Position.X +50 && mousepos.Y > Position.Y -70 && mousepos.Y < Position.Y +70 ){
			return true;
		}
			return false;
		}

}
