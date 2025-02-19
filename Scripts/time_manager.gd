extends Node
class_name TimeManager

signal tick()
signal monthTick()
signal yearTick()

@export_category("Time Settings")
@export_range(1,30) var daysPerTick : int = 1
@export_range(0, 1) var secondsPerTick : float = 0.1
@export_category("Date")

@export var day : int = 1
@export var month : int = 1
@export var year : int = 1

var tickTimer : Timer

func _on_world_worldgen_finished() -> void:
	tickTimer = $"TickTimer"
	tickTimer.wait_time = secondsPerTick	
	tickTimer.start()

func resetTickTimer():
	if (tickTimer):
		tickTimer.wait_time = secondsPerTick
		tickTimer.start()

func _on_tick_timer_timeout() -> void:
	tick.emit()
	day += daysPerTick
	if (day > 30):
		month += int(float(day) / 30)
		day = day - (30 * int(float(day) / 30))
		
		monthTick.emit()
		if (month > 12):
			month = 1
			year += 1
			yearTick.emit()
	
	resetTickTimer()
