extends Node
class_name Culture

var color : Color
var cultureName : String

var population : int
var pops : Array[Pop]

func addPop(pop : Pop):
	if (!pops.has(pop)):
		if (pop.culture != null):
			pop.culture.removePop(pop)
		pops.append(pop)
		pop.culture = self
		population += pop.population

func removePop(pop : Pop):
	if (pops.has(pop)):
		pop.culture = null
		population -= pop.population
		pops.erase(pop)
