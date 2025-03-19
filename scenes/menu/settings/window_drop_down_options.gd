extends Control

@onready var option_button: OptionButton = $OptionButton as OptionButton

enum WindowMode {
	EXCLUSIVE_FULLSCREEN,
	BORDERLESS_FULLSCREEN,
	WINDOWED,
}

func _ready():
	option_button.clear()
	for mode in WindowMode:
		var raw_elements = mode.split("_")
		var modified_elements = []
		for element in raw_elements:
			modified_elements.append(element.to_lower().to_pascal_case())
		option_button.add_item(" ".join(modified_elements), WindowMode[mode])
	match DisplayServer.window_get_mode():
		DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN:
			option_button.select(WindowMode.EXCLUSIVE_FULLSCREEN)
		DisplayServer.WINDOW_MODE_FULLSCREEN:
			option_button.select(WindowMode.BORDERLESS_FULLSCREEN)
		DisplayServer.WINDOW_MODE_WINDOWED:
			option_button.select(WindowMode.WINDOWED)
	
	option_button.item_selected.connect(_on_window_mode_selected)

func _on_window_mode_selected(index : int) -> void:
	match index:
		WindowMode.EXCLUSIVE_FULLSCREEN:
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN)
		WindowMode.BORDERLESS_FULLSCREEN:
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
		WindowMode.WINDOWED:
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	
