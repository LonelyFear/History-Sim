class_name Pop
var simManager : SimManager

# Demography
# Population is 1000 more than its actual value
var population : int = 0
var dependents : int = 0
var workforce : int = 0

static var targetDependencyRatio := 0.75

# Statistics
var birthRate : float = 0.3
var deathRate : float = 0.25
var wealth : float = 0

var region : Object
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

func changeWorkforce(amount : int) -> void:
	if (workforce + amount < 0):
		amount = -workforce
	workforce += amount
	population += amount
	if (region):
		region.changePopulation(amount, 0)
		simManager.changePopulation(amount, 0)

func changeDependents(amount : int) -> void:
	if (dependents + amount < 0):
		amount = -dependents
	dependents += amount
	population += amount
	if (region):
		region.changePopulation(0, amount)
		simManager.changePopulation(0, amount)

func changePopulation(workforce : int, dependents : int) -> void:
	changeWorkforce(workforce)
	changeDependents(dependents)

const simPopulationMultiplier := 1000

static func fromSimPopulation(uP : int) -> int:
	return int(float(uP)/float(simPopulationMultiplier))

static func toSimPopulation(population : int) -> int:
	return population * simPopulationMultiplier
