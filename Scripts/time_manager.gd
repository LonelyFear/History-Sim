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
@export var yearDay : int = 1
@export var month : int = 1
@export var year : int = 1

var tickTimer : Timer

var yearTest : int
func _on_world_worldgen_finished() -> void:
	tickTimer = $"TickTimer"
	tickTimer.wait_time = secondsPerTick
	tickTimer.start()
	yearTest = Time.get_ticks_msec()

func resetTickTimer():
	if (tickTimer):
		tickTimer.wait_time = secondsPerTick
		tickTimer.start()

func _on_tick_timer_timeout() -> void:
	resetTickTimer()

func _process(delta: float) -> void:
	tickGame()

func tickGame():
	tick.emit()
	day += daysPerTick
	yearDay = day + ((month - 1) * 30)
	if (day > 30):
		month += int(float(day) / 30)
		day = day - (30 * int(float(day) / 30))
		
		monthTick.emit()
		if (month > 12):
			month = 1
			year += 1
			yearTick.emit()
			var secondsForYear = float(Time.get_ticks_msec() - yearTest)/1000.0
			yearTest = Time.get_ticks_msec()
			print("Year length (s): " + str(secondsForYear))
