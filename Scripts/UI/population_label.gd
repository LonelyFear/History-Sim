extends Label

@export var simManager : SimManager

func _process(delta: float) -> void:
	if (simManager):
		text = "World Population: " + thousands_sep(simManager.worldPopulation) + "\nWorld Workforce: " + thousands_sep(simManager.worldWorkforce) + "\nWorld Dependents: " + thousands_sep(simManager.worldDependents)+ "\nTotal Pops: " + thousands_sep(simManager.pops.size())
	else:
		visible = false
	

static func thousands_sep(number : int, prefix=''):
	number = int(number)
	var neg = false
	if number < 0:
		number = -number
		neg = true
	var string = str(number)
	var mod = string.length() % 3
	var res = ""
	for i in range(0, string.length()):
		if i != 0 && i % 3 == mod:
			res += ","
		res += string[i]
	if neg: res = '-'+prefix+res
	else: res = prefix+res
	return res
