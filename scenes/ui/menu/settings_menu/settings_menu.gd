extends Control

@export var exit_button: Button
const HAND_POINT = preload("res://assets/ui/cursor/cursor_pointing_hand.png")

func _ready() -> void:
	$TabContainer.get_tab_bar().grab_focus()
	Input.set_custom_mouse_cursor(HAND_POINT, Input.CURSOR_POINTING_HAND)
