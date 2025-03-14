extends Node
class_name TimeManager

signal tick()
signal monthTick()
signal yearTick()

@export_category("Time Settings")
const daysPerTick : int = 30
@export_category("Date")

@export var elapsedTicks : int = 0
@export var day : int = 1
@export var yearDay : int = 1
@export var month : int = 1
@export var year : int = 1
@export_category("References")
@export var simManager : SimManager
var tickTimer : Timer

var tickDeltaStart : int = 0
var tickDeltaTime : float = 0.001

var yearTest : int
func _on_world_worldgen_finished() -> void:
	tickGame()

func _on_tick_timer_timeout() -> void:
	pass

func _process(delta: float) -> void:
	if (WorkerThreadPool.is_group_task_completed(simManager.popTaskId)):
		tickDeltaTime = float(Time.get_ticks_msec() - tickDeltaStart) / 1000
		WorkerThreadPool.wait_for_group_task_completion(simManager.popTaskId)
		tickGame()

func tickGame() -> void:
	tickDeltaStart = Time.get_ticks_msec()
	elapsedTicks += 1
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
			var secondsForYear := float(Time.get_ticks_msec() - yearTest)/1000.0
			yearTest = Time.get_ticks_msec()
