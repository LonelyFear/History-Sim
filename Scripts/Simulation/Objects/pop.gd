extends Node
class_name Pop
var simManager : SimManager

# Demography
var population : int = 0
var dependents : int
var workforce : int
var targetDependencyRatio = 0.75
var birthRate : float = 0.3
var deathRate : float = 0.25

var region : Region
var culture : Culture
var tech : Tech

func changeWorkforce(amount : int):
	if (workforce + amount < 0):
		amount = workforce
	workforce += amount
	population += amount
	if (region):
		region.changePopulation(amount, 0)
		simManager.changePopulation(amount, 0)

func changeDependents(amount : int):
	if (dependents + amount < 0):
		amount = dependents
	dependents += amount
	population += amount
	if (region):
		region.changePopulation(0, amount)
		simManager.changePopulation(0, amount)
