extends Sprite2D

# This code was generated with the assistance of a LLM (https://chatgpt.com/share/6810f383-b830-8012-ab6e-57a479c88f0c)

## The animation track you want the fire to play (will default to 'burn').
@export var animation_name: String = "burn"
## The initial animation offset (in seconds).
## If this variable is larger than the length of the animation. The script will assume the reminder of the animation length divided by this variable.
@export var animation_offset: float = 0.0
## A toogle that, if true, will randomise the animation offset.
## Overrides the manual 'Animation Offset' if true.
@export var randomise_offset: bool = false

func _ready():
	var animation_player = $FlameAnimate
	var animation_length = animation_player.current_animation_length
	
	if randomise_offset or animation_offset > animation_length:
		animation_offset = randf() * animation_player.current_animation_length
	
	animation_player.play(animation_name)
	animation_player.seek(animation_offset, true)
