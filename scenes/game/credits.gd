extends Control

@export var scroll_speed : float = 5.0
@onready var text = $CreditsText
@onready var exit_button = $Exit_Button as Button

func _ready():
	exit_button.button_down.connect(on_exit_pressed)


func _process(_delta: float) -> void:
	#text.position -= Vector2(0.0, scroll_speed*delta)
	if (text.position.y + text.size.y > -100):
		text.position = lerp(text.position, text.position-Vector2(0.0, scroll_speed), 0.1)

func on_exit_pressed() -> void:
	GameManager.SwitchToMainMenu()
