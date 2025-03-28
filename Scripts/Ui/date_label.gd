extends Label

@export var timeManager : Node
func _process(_delta: float) -> void:
	if (timeManager):
		var formatDate := "%02d/%04d"
		var date := formatDate % [timeManager.month, timeManager.year]
		text = date
	else:
		text = ""
