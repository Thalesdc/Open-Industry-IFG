extends Control


func _on_cenario_1_btn_pressed() -> void:
	get_tree().change_scene_to_file("res://src/scenes/scene_1/scene_1.tscn");
	pass # Replace with function body.


func _on_cenario_2_btn_pressed() -> void:
	get_tree().change_scene_to_file("res://src/scenes/scene_3/scene_3.tscn");
	pass # Replace with function body.


func _on_cenario_3_btn_pressed() -> void:
	get_tree().change_scene_to_file("res://src/scenes/scene_2/scene_2.tscn");
	pass # Replace with function body.
