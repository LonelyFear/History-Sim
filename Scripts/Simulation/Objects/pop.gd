extends Node
class_name Pop
var simManager : SimManager

# Demography
var population : int = 0
var dependents : int
var workforce : int
static var targetDependencyRatio = 0.75

# Statistics
var birthRate : float = 0.3
var deathRate : float = 0.25
var wealth : float = 0

var region : Region
var culture : Culture
var tech : Tech
var profession : Professions = Professions.TRIBESPEOPLE

enum Professions {
	TRIBESPEOPLE, # No class
	PEASANT, # Lower Class
	UNEMPLOYED, # Lower Class
	WORKERS, # Lower Class
	SKILLED, # Middle Class
	ARISTOCRATS # Upper Class
}

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

func changePopulation(workforce : int, dependents : int):
	changeWorkforce(workforce)
	changeDependents(dependents)
