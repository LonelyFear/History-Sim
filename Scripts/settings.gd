extends Control

func _on_back_pressed() -> void:
	get_tree().change_scene_to_packed(load("res://Scenes/main_menu.tscn"))
