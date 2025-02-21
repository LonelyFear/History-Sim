extends Node
class_name Pop

var population : int = 1
var birthRate : float = 0.3
var deathRate : float = 0.1
var region : Region
var simManager : SimManager

var updateTick : int 

func changePopulation(amount : int):
	population += amount
	if (region):
		region.population += amount
		simManager.worldPopulation += amount
