extends Control

@export var duration : float = 20
@export var credits_text : Control
@export var skip_button : Button

var credits_tween : Tween

func _ready():
	skip_button.button_down.connect(on_credits_end)
	credits_tween = credits_text.create_tween()
	credits_tween.tween_property(credits_text, "position:y", -(credits_text.size.y + credits_text.position.y), 20)
	credits_tween.tween_callback(on_credits_end)

func on_credits_end() -> void:
	GameManager.SwitchToMainMenu()
