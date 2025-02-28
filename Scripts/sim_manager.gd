extends Node2D
class_name SimManager

@export var world : WorldGenerator
@export var tilesPerRegion : int = 4
@export var regionMap : UpdateTileMapLayer
@export var terrainMap : UpdateTileMapLayer
@export var timeManager : TimeManager
@export var popsPerRegion : int = 60
var tiles : Dictionary
var regions : Dictionary

var terrainSize : Vector2i
var worldSize : Vector2i

# Population
var pops : Array[Pop] = []
@export var worldPopulation : int = 0

var popThread : Thread = Thread.new()

# World size is the amount of regions in the world
func on_worldgen_finished() -> void:
	terrainSize = world.worldSize
	worldSize = terrainSize / tilesPerRegion
	scale = world.scale * tilesPerRegion
	# Adds our subregions to a dictionary
	for x in terrainSize.x:
		for y in terrainSize.y:
			var newTile : Tile = Tile.new()
			tiles[Vector2i(x,y)] = newTile
			
			newTile.biome = world.tileBiomes[Vector2i(x,y)]
	
	for x in worldSize.x:
		for y in worldSize.y:
			regionMap.set_cell(Vector2i(x,y), 0, Vector2i(0,0))
			regionMap.update_tile_color(Vector2i(x,y), Color(0, 0, 0, 0))
			# Creates a region
			var newRegion : Region = Region.new()
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
			
			# Adds pops to our region
			if (newRegion.claimable):
				regionMap.update_tile_color(Vector2i(x,y), Color(randf(), randf(), randf()))
				for i in popsPerRegion:
					createPop(10, newRegion)

func createPop(population : int, region : Region):
	var newPop : Pop = Pop.new()
	newPop.simManager = self
	newPop.region = region
	newPop.updateTick = randi_range(1, 360/timeManager.daysPerTick)
	region.pops.append(newPop)
	newPop.changePopulation(population)
	
	pops.append(newPop)

func _on_tick() -> void:
	updatePops()

func _on_month() -> void:
	#updatePops()
	pass


#region Pops

func updatePops():
	WorkerThreadPool.add_group_task(updatePop, pops.size())

# Grows pop populations
func updatePop(index : int):
	var pop : Pop = pops[index]
	var bRate = pop.birthRate
	
	if (pop.region.population > 1000):
		# If the region is overpopulated apply a 25% decrease to birth rates
		bRate *= 0.75
	if (pop.population < 2):
		bRate *= 0
	# Gets our natural increase trate
	var NIR : float = (bRate - pop.deathRate)
	# Flat increase rate
	var increase = int(float(pop.population) * NIR)
	# Gets the decimal from the NIR multiplied by population and uses it as a chance for one more person to exist
	if (randf() < abs(fmod(float(pop.population) * NIR, 1))):
		# If that person exists (Or dies) change population
		increase += sign(NIR)
	# Updates the pop's population
	pop.changePopulation(increase)
#endregion

func _exit_tree() -> void:
	pass
