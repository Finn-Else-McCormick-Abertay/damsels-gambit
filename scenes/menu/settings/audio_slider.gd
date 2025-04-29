extends Control


@onready var audio_name_lbl: Label = $HBoxContainer/audio_name_lbl as Label
@onready var audio_num_lbl: Label = $HBoxContainer/audio_num_lbl as Label
@onready var h_slider: HSlider = $HBoxContainer/HSlider as HSlider


@export_enum("Master", "Music", "SFX") var bus_name : String

var bus_index : int = 0

func _ready():
	h_slider.value_changed.connect(on_value_changed)
	bus_index = AudioServer.get_bus_index(bus_name)
	update_labels()

func update_labels() -> void:
	audio_name_lbl.text = str(bus_name) + " Volume"
	audio_num_lbl.text = str(h_slider.value * 100)
	h_slider.value = AudioServer.get_bus_volume_linear(bus_index)

#change the audio amount
func on_value_changed(value : float) -> void:
	AudioServer.set_bus_volume_linear(bus_index, value)
	SettingsManager.SetConfig("audio", str(bus_name) + " volume", value)
	update_labels();
