extends Label

@export var timeManager : Node
var currentTimer : float = 0
func _process(delta: float) -> void:
	if (timeManager):
		currentTimer -= delta
		if (currentTimer <= 0):
			text = "TPS: " + str(roundf(1/timeManager.tickDelta * 10) / 10) + "\nFPS: " + str(roundf(1/delta * 10) / 10)
			currentTimer = 0.5
	else:
		text = ""
