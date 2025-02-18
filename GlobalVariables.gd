extends Node

signal simulation_started
signal simulation_set_paused(paused)
signal simulation_ended
signal opc_da_comms_connected

var opc_da_connected: bool = false

var main : Node

#func _ready() -> void:
	#print("_ready")
	#
	#print("EditorInterface")
	#
	#main = get_node("cena_1")
	#print("is_instance_valid(main)")
	#print(is_instance_valid(main))
	#
	#print("is_instance_valid(owner)")
	#print(is_instance_valid(owner))
	#if is_instance_valid(owner):
		#await owner.ready
		#EditorInterface.set_main_screen_editor("3D")
		#EditorInterface.open_scene_from_path("res://cena_1.tscn")
