extends Label

@export var timeManager : Node
@export var updateDelay : float = 0.5
var currentTimer : float = 0
func _process(delta: float) -> void:
	if (timeManager):
		currentTimer -= delta
		if (currentTimer <= 0):
			text = "TPS: " + str(roundf(1/timeManager.tickDelta * 10) / 10) + "\nMTPS: " + str(roundf(1/timeManager.monthDelta * 10) / 10) + "\nFPS: " + str(roundf(1/delta * 10) / 10)
			currentTimer = updateDelay
	else:
		text = ""
