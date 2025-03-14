extends Node
class_name Culture

var color : Color
var cultureName : String

var population : int
var pops : Array[Pop]

func addPop(pop : Pop) -> void:
	if (!pops.has(pop)):
		if (pop.culture != null):
			pop.culture.removePop(pop)
		pops.append(pop)
		pop.culture = self
		population += pop.population

func removePop(pop : Pop) -> void:
	if (pops.has(pop)):
		pop.culture = null
		population -= pop.population
		pops.erase(pop)

static func SimilarCulture(cultureA : Culture, cultureB : Culture) -> bool:
	var minColorDiff : float = 0.05
	var similarR : bool = abs(cultureA.color.r - cultureB.color.r) < minColorDiff
	var similarG : bool = abs(cultureA.color.g - cultureB.color.g) < minColorDiff	
	var similarB : bool = abs(cultureA.color.b - cultureB.color.b) < minColorDiff
	return similarR && similarG && similarB
