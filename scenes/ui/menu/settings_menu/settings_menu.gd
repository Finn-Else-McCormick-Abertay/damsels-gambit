extends Control

@export var exit_button: Button

func _ready() -> void:
	$TabContainer.get_tab_bar().grab_focus()
