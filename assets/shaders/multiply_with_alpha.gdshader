shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

void fragment() {
	vec4 input_color = COLOR;
	vec4 screen_sample = texture(screen_texture, SCREEN_UV);
	
	COLOR = input_color * screen_sample;
}