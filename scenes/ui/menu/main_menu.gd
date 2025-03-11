extends Control

@export var start_button: TextureButton
@export var settings_button: TextureButton
@export var exit_button: TextureButton
@export var button_root: Control
@export var settings_menu_scene: PackedScene

var settings_menu: Control

func _ready():
	start_button.button_down.connect(_on_start_pressed)
	settings_button.button_down.connect(_on_settings_pressed)
	exit_button.button_down.connect(_on_exit_pressed)
	start_button.grab_focus()

func _on_start_pressed()-> void:
	GameManager.BeginGame()

func _on_settings_pressed()-> void:
	InputManager.PushToFocusStack()
	
	if is_instance_valid(settings_menu):
		settings_menu.queue_free()
		settings_menu = null
	
	settings_menu = settings_menu_scene.instantiate()
	add_child(settings_menu)
	settings_menu.owner = self
	
	(settings_menu.get("exit_button") as Button).button_down.connect(_on_exit_settings_menu)
	
	button_root.hide()

func _on_exit_pressed() -> void:
	get_tree().quit()

func _on_exit_settings_menu() -> void:
	button_root.show()
	if is_instance_valid(settings_menu):
		settings_menu.hide()
		settings_menu.queue_free()
		settings_menu = null
	InputManager.PopFromFocusStack()
