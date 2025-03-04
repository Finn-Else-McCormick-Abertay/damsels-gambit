extends Control


@onready var state_label: Label = $HBoxContainer/state_label
@onready var check_box: CheckBox = $HBoxContainer/CheckBox

func _ready():
	check_box.toggled.connect(on_dyslexia_toggled)

func set_label_text(button_pressed : bool) -> void:
	if button_pressed != true: 
		state_label.text = "Off"
	else:
		state_label.text = "On"



func on_dyslexia_toggled(button_pressed : bool) -> void:
	set_label_text(button_pressed) 
	
