extends Control

@export var scroll_speed : float = 5.0
@onready var text = $CreditsText




func _process(delta: float) -> void:
	#text.position -= Vector2(0.0, scroll_speed*delta)
	if (text.position.y + text.size.y > -100):
		text.position = lerp(text.position, text.position-Vector2(0.0, scroll_speed), 0.1)
