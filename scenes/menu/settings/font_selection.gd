extends Control

@onready var option_button_2: OptionButton = $OptionButton2

@onready var FONT_DICTIONARY : Dictionary = {
	"Vinque" : ResourceLoader.load("res://assets/fonts/vinque/Vinque Rg.otf", "Font"),
	"Open Dyslexic" : ResourceLoader.load("res://assets/fonts/vinque/Vinque Rg.otf", "Font"),
	"Argos George" : ResourceLoader.load("res://assets/fonts/vinque/Vinque Rg.otf", "Font")
}

func _ready():
	option_button_2.item_selected.connect(on_font_selected)
	option_button_2.clear()
	for font in FONT_DICTIONARY:
		option_button_2.add_item(font)

func on_font_selected(_index : int) -> void:
	#FONT_DICTIONARY.values()[index]
	pass
