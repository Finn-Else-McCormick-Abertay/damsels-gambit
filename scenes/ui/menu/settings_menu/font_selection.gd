extends Control

@onready var option_button_2: OptionButton = $OptionButton2

const FONT_DICTIONARY : Dictionary = {
	"Vinque" :
	"Open Dyslexic" :
	"Argos George" :
}

func _ready():
	option_button.item_selected.connect(on_resolution_selected)
	option_button.clear()
	for resolution_size_text in FONT_DICTIONARY:
		option_button.add_item(resolution_size_text)

func on_resolution_selected(index : int) -> void:
	DisplayServer.window_set_size(FONT_DICTIONARY.values()[index])extends HBoxContainer
