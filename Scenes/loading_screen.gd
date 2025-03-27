extends Control

var gameScenePath : String = "res://Scenes/game.tscn"
var time : float = 0
var dotCount : int = 0
func _ready() -> void:
	ResourceLoader.load_threaded_request(gameScenePath)

func _process(delta: float) -> void:
	var progress := []
	ResourceLoader.load_threaded_get_status(gameScenePath, progress)
	$"ProgressBar".value = progress[0] * 100
	
	if (progress[0] == 1):
		var packedScene : PackedScene = ResourceLoader.load_threaded_get(gameScenePath)
		get_tree().change_scene_to_packed(packedScene)
