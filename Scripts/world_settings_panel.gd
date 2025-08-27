extends Panel

@onready var lineEdit : LineEdit = $"VBoxContainer/SeedContainer/Seed"
@onready var worldSizeDropdown : OptionButton = $VBoxContainer/WorldSizeContainer/WorldSize
var old_text := ""

func _on_seed_text_changed(new_text: String) -> void:
	if (new_text.is_empty() or new_text.is_valid_int()):
		old_text = new_text
	else:
		lineEdit.text = old_text


func _on_start_pressed() -> void:
	var worldSeed : int = 0
	if (lineEdit.text.is_empty()):
		worldSeed = randi_range(0, 999999999)
	else:
		worldSeed = lineEdit.text.to_int()
	var game : Node2D = load("res://Scenes/game.tscn").instantiate()
	game.get_node("Loading/Loading Screen").seed = worldSeed
	game.get_node("Loading/Loading Screen").worldMult = worldSizeDropdown.get_selected_id()
	get_tree().root.add_child(game)
	get_parent().queue_free()


func _on_back_pressed() -> void:
	get_tree().change_scene_to_packed(load("res://Scenes/main_menu.tscn"))
