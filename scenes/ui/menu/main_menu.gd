class_name MainMenu
extends Control

@onready var start_button: TextureButton = $MarginContainer/VBoxContainer/HBoxContainer/start_button as TextureButton
@onready var settings_button: TextureButton = $MarginContainer/VBoxContainer/HBoxContainer/settings_button as TextureButton
@onready var exit_button: TextureButton = $MarginContainer/VBoxContainer/HBoxContainer/exit_button as TextureButton
@onready var settings_menu_acessibility: SettingsMenu = $SettingsMenuAcessibility as SettingsMenu 
@onready var margin_container: MarginContainer = $MarginContainer
@onready var start_level: = preload("res://scenes/main.tscn") as PackedScene

func _ready():
	handle_connecting_signals()

func on_start_pressed()-> void:
	get_tree().change_scene_to_packed(start_level)

func on_settings_pressed()-> void:
	margin_container.visible = false
	settings_menu_acessibility.set_process(true)
	settings_menu_acessibility.visible = true

func on_exit_pressed() -> void:
	get_tree().quit()

func on_exit_settings_menu() -> void:
	margin_container.visible = true
	settings_menu_acessibility.visible = false

func handle_connecting_signals() -> void:
	start_button.button_down.connect(on_start_pressed)
	settings_button.button_down.connect(on_settings_pressed)
	exit_button.button_down.connect(on_exit_pressed)
	settings_menu_acessibility.exit_settings_menu_acessibility.connect(on_exit_settings_menu)
