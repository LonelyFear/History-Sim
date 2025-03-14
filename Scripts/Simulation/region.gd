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


func calcMaxPopulation() -> void:
	# Gets max population
	maxPopulation = 0
	for bpos in biomes:
		var biome : Dictionary = biomes[bpos]
		var tile : Tile = tiles[bpos]
		# Checks if the biome is land
		tile.maxPopulation = 0
		if (biome["terrainType"] == 0):
			# If land sets max population for tile and adds population to region
			maxPopulation += Pop.toSimPopulation(1000) * biome["fertility"]
			tile.maxPopulation = Pop.toSimPopulation(1000) * biome["fertility"]

func calcAvgFertility() -> void:
	landCount = 0
	var f : float = 0
	for biome : Dictionary in biomes.values():
		# Checks if the biome is land
		if (biome["terrainType"] == 0):
			landCount += 1
			f += biome["fertility"]
	
	avgFertility = (f/landCount)

func changePopulation(workforceIncrease : int, dependentIncrease : int) -> void:
	workforce += workforceIncrease
	dependents += dependentIncrease
	population = workforce + dependents

func removePop(pop : Pop) -> void:
	if (pops.has(pop)):
		changePopulation(-pop.workforce, -pop.dependents)
		pops.erase(pop)
		pop.region = null

func addPop(pop : Pop) -> void:
	if (!pops.has(pop)):
		changePopulation(pop.workforce, pop.dependents)
		pops.append(pop)
		pop.region = self

func growPops() -> void:
	for pop : Pop in pops:
		var bRate : float
		if (pop.population < 2):
			bRate = 0
		else:
			bRate = pop.birthRate
		if (population > maxPopulation):
			# If the region is overpopulated apply a 25% decrease to birth rates
			bRate *= 0.75
		
		# Gets our natural increase trate
		var NIR : float = (bRate - pop.deathRate) / (360/TimeManager.daysPerTick)
		# Gets our increase
		var increase : int = (pop.dependents + pop.workforce) * NIR
		# Gets our increase in dependents
		var dependentIncrease : int = increase * pop.targetDependencyRatio
		# Has dependents age into workforce (25%)
		pop.changeWorkforce(increase - dependentIncrease)
		# Has dependents born (75%)
		pop.changeDependents(dependentIncrease)
