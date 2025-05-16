extends Label

@export var timeManager : Node
func _process(_delta: float) -> void:
	if (timeManager):
		text = timeManager.GetStringDate(0)
	else:
		text = ""
