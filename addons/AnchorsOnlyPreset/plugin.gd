@tool
extends EditorPlugin

var picker: Node
var selected_signal: Signal
var selected_callback: Callable

var toggle: CheckButton

var i18n_handler: RefCounted

func _enter_tree() -> void:
	if picker == null:
		picker = get_editor_interface().get_base_control().find_child("@AnchorPresetPicker@*", true, false)
		assert(picker.get_class() == "AnchorPresetPicker")
		selected_signal = picker.get("anchors_preset_selected")
		var con = selected_signal.get_connections()
		selected_callback = con[0]["callable"]
	
	selected_signal.disconnect(selected_callback)
	selected_signal.connect(wrapped_selected)
	
	if ClassDB.class_exists("TranslationDomain"): # Add translation for Godot 4.4+
		var path = (self.get_script() as GDScript).resource_path.get_base_dir().path_join("i18n.gd")
		i18n_handler = load(path).new()
		i18n_handler.register()
	
	toggle = CheckButton.new()
	toggle.text = "Anchors Only"
	
	var vbox = picker.get_parent()
	vbox.add_child(toggle)
	vbox.move_child(toggle, 1)

func wrapped_selected(preset: int):
	if not toggle.button_pressed:
		selected_callback.call(preset)
		return
	
	var undo := get_undo_redo()
	undo.create_action(tr("Change Anchors, Grow Direction"))
	
	var selection = get_editor_interface().get_selection().get_selected_nodes()
	for node in selection:
		var control := node as Control
		if control:
			undo.add_do_property(control, "layout_mode", 1)
			undo.add_do_method(control, "set_anchors_preset", preset, false)
			undo.add_do_method(control, "set_h_grow_direction", get_h_grow_direction(preset))
			undo.add_do_method(control, "set_v_grow_direction", get_v_grow_direction(preset))
			undo.add_undo_method(control, "_edit_set_state", control.call("_edit_get_state"))
	
	undo.commit_action()

func get_h_grow_direction(preset):
	match preset:
		Control.PRESET_TOP_LEFT, Control.PRESET_BOTTOM_LEFT, Control.PRESET_CENTER_LEFT, Control.PRESET_LEFT_WIDE:
			return Control.GROW_DIRECTION_END
		Control.PRESET_TOP_RIGHT, Control.PRESET_BOTTOM_RIGHT, Control.PRESET_CENTER_RIGHT, Control.PRESET_RIGHT_WIDE:
			return Control.GROW_DIRECTION_BEGIN
		Control.PRESET_CENTER_TOP, Control.PRESET_CENTER_BOTTOM, Control.PRESET_CENTER, Control.PRESET_TOP_WIDE, \
		Control.PRESET_BOTTOM_WIDE, Control.PRESET_VCENTER_WIDE, Control.PRESET_HCENTER_WIDE, Control.PRESET_FULL_RECT:
			return Control.GROW_DIRECTION_BOTH

func get_v_grow_direction(preset):
	match preset:
		Control.PRESET_TOP_LEFT, Control.PRESET_TOP_RIGHT, Control.PRESET_CENTER_TOP, Control.PRESET_TOP_WIDE:
			return Control.GROW_DIRECTION_END
		Control.PRESET_BOTTOM_LEFT, Control.PRESET_BOTTOM_RIGHT, Control.PRESET_CENTER_BOTTOM, Control.PRESET_BOTTOM_WIDE:
			return Control.GROW_DIRECTION_BEGIN
		Control.PRESET_CENTER_LEFT, Control.PRESET_CENTER_RIGHT, Control.PRESET_CENTER, Control.PRESET_LEFT_WIDE, \
		Control.PRESET_RIGHT_WIDE, Control.PRESET_VCENTER_WIDE, Control.PRESET_HCENTER_WIDE, Control.PRESET_FULL_RECT:
			return Control.GROW_DIRECTION_BOTH

func _exit_tree() -> void:
	if picker != null:
		selected_signal.disconnect(wrapped_selected)
		selected_signal.connect(selected_callback)
	
	if toggle:
		toggle.queue_free()
		toggle = null
	
	if i18n_handler:
		i18n_handler.unregister()
		i18n_handler = null
