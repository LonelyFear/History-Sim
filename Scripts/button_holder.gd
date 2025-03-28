extends VBoxContainer


func _on_start_pressed() -> void:
	get_tree().change_scene_to_packed(load("res://Scenes/game.tscn"))


func _on_exit_game_pressed() -> void:
	get_tree().quit()
