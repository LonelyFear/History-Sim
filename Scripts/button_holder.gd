extends VBoxContainer

var closeConfirm : ConfirmationDialog 

func _on_start_pressed() -> void:
	get_tree().change_scene_to_packed(load("res://Scenes/world_generation_screen.tscn"))
func _on_exit_game_pressed() -> void:
	get_tree().quit()
func _on_load_pressed() -> void:
	get_tree().change_scene_to_packed(load("res://Scenes/save_selection.tscn"))
