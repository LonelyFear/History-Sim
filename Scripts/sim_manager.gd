extends Node2D
class_name SimManager

@export var world : WorldGenerator
@export var tilesPerRegion : int = 4
@export var regionMap : UpdateTileMapLayer
@export var terrainMap : UpdateTileMapLayer

@export var popsPerRegion : int = 20
var tiles : Dictionary
var regions : Dictionary
var terrainSize : Vector2i
var worldSize : Vector2i

# Threads
var popThread1 : Thread = Thread.new()
var popThread2 : Thread = Thread.new()
var popThread3 : Thread = Thread.new()
var popThread4 : Thread = Thread.new()

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
					var newPop : Pop = Pop.new()
					# Adds the region to our pop
					newPop.region = newRegion
					# And pop to our region
					newRegion.pops.append(newPop)


func _on_tick() -> void:
	updatePops()

func updatePops():
	var pops1 : Array[Pop] = []
	var pops2 : Array[Pop] = []
	var pops3 : Array[Pop] = []
	var pops4 : Array[Pop] = []
	
	var iterator : int = 0
	for region in regions.values():
		for pop : Pop in region.pops:
			match iterator:
				0:
					pops1.append(pop)
				1:
					pops2.append(pop)
		iterator += 1
		if (iterator > 0):
			iterator = 0
	
	updatePopArray(pops1)
	#popThread1.start(updatePopArray.bind(pops2))
	#popThread1.wait_to_finish()


func updatePopArray(pops : Array[Pop]):
	for pop : Pop in pops:
		var NIR : float = pop.birthRate - pop.deathRate
		var increase = int(float(pop.population) * NIR)
		if (randf() < abs(fmod(float(pop.population) * NIR, 1))):
			increase += sign(NIR)
		pop.changePopulation(increase)
	
