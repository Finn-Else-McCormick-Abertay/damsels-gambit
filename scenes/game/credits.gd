extends Control

@export var duration : float = 20
@export var credits_text : Control
@export var skip_button : Button

var credits_tween : Tween

func _ready() -> void:
	# Start out skip button as hidden
	skip_button.hide()
	skip_button.button_down.connect(on_credits_end)

	credits_tween = credits_text.create_tween()
	credits_tween.tween_property(credits_text, "position:y", -(credits_text.size.y + credits_text.position.y), 20)
	credits_tween.tween_callback(on_credits_end)

func _input(event: InputEvent) -> void:
	# Display skip button if input recieved
	if event is not InputEventMouseMotion && event is not InputEventScreenDrag:
		skip_button.show()

func on_credits_end() -> void:
	GameManager.SwitchToMainMenu()
