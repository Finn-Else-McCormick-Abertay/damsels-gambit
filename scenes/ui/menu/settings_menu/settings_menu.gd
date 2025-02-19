class_name SettingsMenu
extends Control

@onready var exit_button: Button = $MarginContainer/VBoxContainer/exit_button as Button

signal exit_settings_menu_acessibility

func _ready():
	exit_button.button_down.connect(on_exit_pressed)
	set_process(false) 

func on_exit_pressed() -> void:
	exit_settings_menu_acessibility.emit()
	set_process(false)
