extends CanvasLayer

var gameScenePath : String = "res://Scenes/game.tscn"
var time : float = 0
var dotCount : int = 0
var world : Node2D
var th : Thread = Thread.new()

func _ready() -> void:
	pass
	world = load("res://Scenes/game.tscn").instantiate()
	get_tree().root.add_child(world)
	#world.visible = false


func _process(_delta : float) -> void:
	print("a")
	#if th==null:
		#return
	#if !th.is_started():
		#th.start(world.GenerateWorld)
	#if !th.is_alive():
		#th.wait_to_finish()
		#th=null
		#queue_free()
