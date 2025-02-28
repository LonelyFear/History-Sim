extends Label

@export var timeManager : TimeManager
func _process(delta: float) -> void:
	if (timeManager):
		var formatDate = "%02d/%02d/%04d"
		var date = formatDate % [timeManager.day, timeManager.month, timeManager.year]
		if (timeManager.daysPerTick >= 30):
			formatDate = "%02d/%04d"
			date = formatDate % [timeManager.month, timeManager.year]
		text = date
	else:
		text = ""
