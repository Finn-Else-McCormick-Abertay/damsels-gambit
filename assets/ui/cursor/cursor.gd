extends Control

var cursor = preload("res://assets/ui/cursor/cursor_arrow.png")
var cursor_pointing_hand = preload("res://assets/ui/cursor/cursor_pointing_hand.png")

func _ready():
	Input.set_custom_mouse_cursor(cursor, Input.CURSOR_ARROW)
	Input.set_custom_mouse_cursor(cursor_pointing_hand, Input.CURSOR_POINTING_HAND)
	
func _on_play_hand_button_mouse_entered() -> void:
	Input.set_custom_mouse_cursor(cursor_pointing_hand, Input.CURSOR_POINTING_HAND)
	
func _on_play_hand_button_mouse_exited() -> void:
	Input.set_custom_mouse_cursor(cursor, Input.CURSOR_ARROW)
	
func _on_card_display_mouse_entered() -> void:
	Input.set_custom_mouse_cursor(cursor_pointing_hand, Input.CURSOR_POINTING_HAND)

func _on_card_display_mouse_exited() -> void:
	Input.set_custom_mouse_cursor(cursor, Input.CURSOR_ARROW)

func _on_card_display_2_mouse_entered() -> void:
	Input.set_custom_mouse_cursor(cursor_pointing_hand, Input.CURSOR_POINTING_HAND)
	
func _on_card_display_2_mouse_exited() -> void:
	Input.set_custom_mouse_cursor(cursor, Input.CURSOR_ARROW)
	
func _on_card_display_3_mouse_entered() -> void:
	Input.set_custom_mouse_cursor(cursor_pointing_hand, Input.CURSOR_POINTING_HAND)
	
func _on_card_display_3_mouse_exited() -> void:
	Input.set_custom_mouse_cursor(cursor, Input.CURSOR_ARROW)
	
func _on_card_display_4_mouse_entered() -> void:
	Input.set_custom_mouse_cursor(cursor_pointing_hand, Input.CURSOR_POINTING_HAND)

func _on_card_display_4_mouse_exited() -> void:
	Input.set_custom_mouse_cursor(cursor, Input.CURSOR_ARROW)
