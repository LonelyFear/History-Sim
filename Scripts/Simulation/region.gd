extends Node
class_name Region

var tiles : Dictionary[Vector2i, Tile]
var biomes : Dictionary[Vector2i, Dictionary]
var claimable : bool = false
var pops : Array[Pop]

var pos : Vector2i
var avgFertility : float
var landCount : int = 0

# Demographics
var maxPopulation : int = 10000
var population : int = 0
var workforce : int = 0
var dependents : int = 0


func calcMaxPopulation():
	# Gets max population
	maxPopulation = 0
	for pos in biomes:
		var biome : Dictionary = biomes[pos]
		var tile : Tile = tiles[pos]
		# Checks if the biome is land
		tile.maxPopulation = 0
		if (biome["terrainType"] == 0):
			# If land sets max population for tile and adds population to region
			maxPopulation += 1000 * biome["fertility"]
			tile.maxPopulation = 1000 * biome["fertility"]

func calcAvgFertility():
	landCount = 0
	var f : float = 0
	for biome in biomes.values():
		# Checks if the biome is land
		if (biome["terrainType"] == 0):
			landCount += 1
			f += biome["fertility"]
	
	avgFertility = (f/landCount)

func changePopulation(workforceIncrease : int, dependentIncrease : int):
	workforce += workforceIncrease
	dependents += dependentIncrease
	population = workforce + dependents

func removePop(pop : Pop):
	if (pops.has(pop)):
		changePopulation(-pop.workforce, -pop.dependents)
		pops.erase(pop)
		pop.region = null

func addPop(pop : Pop):
	if (!pops.has(pop)):
		changePopulation(pop.workforce, pop.dependents)
		pops.append(pop)
		pop.region = self
