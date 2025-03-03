extends Control


@onready var audio_name_lbl: Label = $HBoxContainer/audio_name_lbl as Label
@onready var audio_num_lbl: Label = $HBoxContainer/audio_num_lbl as Label
@onready var h_slider: HSlider = $HBoxContainer/HSlider as HSlider

@export_enum() var bus_name : String
