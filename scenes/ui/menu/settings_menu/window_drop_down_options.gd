extends Control

@onready var option_button: OptionButton = $OptionButton as OptionButton

const WINDOW_MODE_ARRAY :Array[String] = [
	"Fullscreen",
	"Borderless Fullscreen",
	"Windowed",
	"Borderless Window",
]

func _ready():
	option_button.clear()
	for window_mode in WINDOW_MODE_ARRAY:
		option_button.add_item(window_mode)
	match DisplayServer.window_get_mode():
		DisplayServer.WINDOW_MODE_FULLSCREEN:
			option_button.select(1 if DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_BORDERLESS) else 0)
		DisplayServer.WINDOW_MODE_WINDOWED:
			option_button.select(3 if DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_BORDERLESS) else 2)
	
	option_button.item_selected.connect(_on_window_mode_selected)

func _on_window_mode_selected(index : int) -> void:
	match index:
		0: #fullscreen
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, false)
		1: #Window Mode
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, false)
		2: #Borderless Window
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, true)
		3: #Borderless Fullscreen
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, true)
	
