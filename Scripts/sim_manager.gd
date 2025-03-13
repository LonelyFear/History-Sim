extends Node2D
class_name SimManager

@export var world : WorldGenerator
@export var tilesPerRegion : int = 4
@export var regionSprite : Sprite2D
@export var terrainMap : UpdateTileMapLayer
@export var timeManager : TimeManager
@export var popsPerRegion : int = 50

var tiles : Dictionary[Vector2i, Tile]
var regions : Dictionary[Vector2i, Region]

var terrainSize : Vector2i
var worldSize : Vector2i

var regionImage : Image

# Population
var pops : Array[Pop] = []

var worldPopulation : int = 0
var worldWorkforce : int = 0
var worldDependents : int = 0
var cultures : Array[Culture] = []

var popThread : Thread = Thread.new()

var mapUpdate : bool = false

var popTaskId : int = 0

# World size is the amount of regions in the world
func on_worldgen_finished() -> void:
	terrainSize = world.worldSize
	worldSize = terrainSize / tilesPerRegion
	scale = world.scale * tilesPerRegion
	regionImage = Image.create(worldSize.x, worldSize.y, true, Image.FORMAT_RGBA8)
	# Adds our subregions to a dictionary
	for x in terrainSize.x:
		for y in terrainSize.y:
			var newTile : Tile = Tile.new()
			tiles[Vector2i(x,y)] = newTile
			
			newTile.biome = world.tileBiomes[Vector2i(x,y)]
	
	for x in worldSize.x:
		for y in worldSize.y:
			regionImage.set_pixel(x,y, Color(0,0,0,0))
			# Creates a region
			var newRegion : Region = Region.new()
			newRegion.pos = Vector2i(x,y)
			regions[Vector2i(x,y)] = newRegion
			for tx in tilesPerRegion:
				for ty in tilesPerRegion:
					# Adds subregion tiles
					var tile : Tile = tiles[Vector2i(x * tilesPerRegion + tx,y * tilesPerRegion + ty)]
					newRegion.tiles[Vector2i(tx, ty)] = tile
					# Adds biomes to tile
					newRegion.biomes[Vector2i(tx, ty)] = tile.biome
			# Checks if our region is claimable
			for biome in newRegion.biomes.values():
				if (biome["terrainType"] == 0):
					newRegion.claimable = true
					break
			# Calculates average fertility of region
			newRegion.calcAvgFertility()
			# Calculates max population of region
			newRegion.calcMaxPopulation()
			# Adds pops to our region
			if (newRegion.claimable):
				for i in popsPerRegion:
					var startingPopulation : int = 10
					createPop(startingPopulation * (1.0 - Pop.targetDependencyRatio), startingPopulation * (Pop.targetDependencyRatio), newRegion, Tech.new(), createCulture(newRegion))
	regionSprite.texture = ImageTexture.create_from_image(regionImage)

func createPop(workforce : int, dependents : int, region : Region, tech : Tech, culture : Culture, profession : Pop.Professions = Pop.Professions.TRIBESPEOPLE) -> Pop:
	# Creates a new pop
	var newPop : Pop = Pop.new()
	newPop.simManager = self
	
	# Adds pop to region
	newPop.region = region
	region.pops.append(newPop)
	
	# Changes population
	newPop.changeWorkforce(workforce)
	newPop.changeDependents(dependents)
	
	# Adds culture
	culture.addPop(newPop)
	pops.append(newPop)
	
	# Adds profession
	newPop.profession
	
	# Adds tech
	var popTech : Tech = Tech.new()
	popTech.industryLevel = tech.industryLevel
	popTech.militaryLevel = tech.militaryLevel
	popTech.societyLevel = tech.societyLevel
	newPop.tech = popTech
	
	return newPop

func _on_tick() -> void:
	mapUpdate = false
	updatePops()
	#worldPopulation = worldDependents + worldWorkforce
	if (mapUpdate):
		regionSprite.texture = ImageTexture.create_from_image(regionImage)

func getRegion(pos : Vector2i) -> Region:
	return regions[pos]

func _on_month() -> void:
	#updatePops()
	pass


#region Pops

func changePopulation(workforceIncrease : int, dependentIncrease : int):
	worldWorkforce += workforceIncrease
	worldDependents += dependentIncrease
	worldPopulation = worldWorkforce + worldDependents

func updatePops():
	popTaskId = WorkerThreadPool.add_group_task(growPop, pops.size(), 4, false, "Grows all the pops in the simulation")

# Grows pop populations
func growPop(index : int):
	var pop : Pop = pops[index]
	var bRate = pop.birthRate

	if (pop.region.population > pop.region.maxPopulation):
		# If the region is overpopulated apply a 25% decrease to birth rates
		bRate *= 0.75
	if (pop.population < 2):
		bRate = 0
	# Gets our natural increase trate
	var NIR : float = (bRate - pop.deathRate)/12
	# Flat increase rate
	var increase : int = int(float(pop.population) * NIR)
	# Gets the decimal from the NIR multiplied by population and uses it as a chance for one more person to exist
	if (randf() < abs(fmod(float(pop.population) * NIR, 1))):
		# If that person is born (Or dies) change population
		increase += sign(NIR)
	# Updates the pop's population
	
	# Gets dependent change
	var dependentIncrease : int = int(float(increase) * pop.targetDependencyRatio)
	# Checks if an dependent is born
	if (randf() < abs(fmod(float(increase) * pop.targetDependencyRatio, 1))):
		dependentIncrease += sign(increase)
	# Has dependents age into workforce (25%)
	pop.changeWorkforce(increase - dependentIncrease)
	# Has dependents born (75%)
	pop.changeDependents(dependentIncrease)

func MovePop(pop : Pop, destination : Region, workforce : int, dependents : int, profession : Pop.Professions):
	if (destination != null):
		if (workforce > pop.workforce):
			workforce = pop.workforce
		if (dependents > pop.dependents):
			dependents = pop.dependents
		if (workforce + dependents >= pop.workforce + pop.dependents):
			pop.profession = profession
			pop.region.removePop(pop)
			destination.addPop(pop)
		else:
			var similarPop : Pop
			var createPop : bool = true
			for rPop : Pop in destination.pops:
				if (Culture.SimilarCulture(pop.culture, rPop.culture) && profession == rPop.profession):
					createPop = false
					similarPop = rPop
					break
			if createPop:
				var newPop : Pop = createPop(workforce, dependents, destination, pop.tech, pop.culture, profession)
			elif similarPop != null:
				similarPop.changePopulation(workforce, dependents)
		pop.changePopulation(-workforce, -dependents)
#endregion

#region Cultures
func createCulture(region : Region) -> Culture:
	var newCulture : Culture = Culture.new()
	newCulture.name = "New Culture"
	var r = inverse_lerp(0, worldSize.x, region.pos.x)
	var g = inverse_lerp(0, worldSize.y, region.pos.y)
	var b = inverse_lerp(0, worldSize.y, region.pos.y - worldSize.x)
	newCulture.color = Color(r, g, b, 0)
	cultures.append(newCulture)
	
	return newCulture
#endregion

#region MapEdit
func setRegionColor(pos : Vector2i, color : Color):
	regionImage.set_pixel(pos.x, pos.y, Color(color, 1))
	mapUpdate = true
#endregion

func _exit_tree() -> void:
	pass
