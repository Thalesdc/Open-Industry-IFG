extends CharacterBody3D

@export var max_speed: float = 10.0
@export var acceleration: float = 25.0

@export var camera: Camera3D

func _physics_process(delta: float) -> void:
	
	## --- Physics --- ##
	
	# Get the input direciton and handle movement/deceleration
	var input_dir := Input.get_vector(&"move_left", &"move_right", &"move_forward", &"move_back")
	var direction := Vector3(input_dir.x, 0, input_dir.y).normalized()
	
	## --- Movement --- ##
	# Rotates towards camera
	direction = direction.rotated(Vector3.UP, camera.global_rotation.y)
	
	if direction:
		#velocity.x = direction.x * max_speed
		#velocity.z = direction.z * max_speed
		direction *= max_speed
		velocity.x = move_toward(velocity.x, direction.x, delta * acceleration)
		velocity.z = move_toward(velocity.z, direction.z, delta * acceleration)
	else:
		velocity.x = move_toward(velocity.x, 0, delta * acceleration)
		velocity.z = move_toward(velocity.z, 0, delta * acceleration)
		
		
	move_and_slide()
