extends Node2D
class_name SimManager

@onready var world : WorldGenerator
@export var tilesPerRegion : int = 4
@onready var regionSprite : Sprite2D = $"RegionOverlay"
@onready var timeManager : TimeManager 
@export var popsPerRegion : int = 1

var tiles : Dictionary[Vector2i, Tile]
var regions : Array[Object]
var terrainSize : Vector2i
var worldSize : Vector2i

var regionImage : Image
var regionObj : Object = preload("res://Scripts/Simulation/Region.cs")

# Population
var pops : Array[Pop] = []

var worldPopulation : int = 0
var worldWorkforce : int = 0
var worldDependents : int = 0
var cultures : Array[Culture] = []

var mapUpdate : bool = false

var currentBatchNum : int = 0
var popTaskId : int = 0

func _ready() -> void:
	world = get_parent().find_child("World")
	timeManager = get_parent().find_child("Time Manager")

# World size is the amount of regions in the world
func on_worldgen_finished() -> void:
	terrainSize = world.worldSize
	worldSize = terrainSize / tilesPerRegion
	scale = world.scale * tilesPerRegion
	regionImage = Image.create_empty(worldSize.x, worldSize.y, true, Image.FORMAT_RGBA8)
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
			newRegion.simManager = self
			newRegion.pos = Vector2i(x,y)
			regions.append(newRegion)
			for tx in tilesPerRegion:
				for ty in tilesPerRegion:
					# Adds subregion tiles
					var tile : Tile = tiles[Vector2i(x * tilesPerRegion + tx,y * tilesPerRegion + ty)]
					newRegion.tiles[Vector2i(tx, ty)] = tile
					# Adds biomes to tile
					newRegion.biomes[Vector2i(tx, ty)] = tile.biome
					if (tile.biome["terrainType"] == 0):
						newRegion.claimable = true
			# Calculates average fertility of region
			newRegion.calcAvgFertility()
			# Calculates max population of region
			newRegion.calcMaxPopulation()
			# Adds pops to our region
			if (newRegion.claimable):
				for i in popsPerRegion:
					var startingPopulation : int = Pop.toSimPopulation(20)
					createPop(startingPopulation * (1.0 - Pop.targetDependencyRatio), startingPopulation * (Pop.targetDependencyRatio), newRegion, Tech.new(), createCulture(newRegion))
	regionSprite.texture = ImageTexture.create_from_image(regionImage)

func createPop(workforce : int, dependents : int, region : Object, tech : Tech, culture : Culture, profession : Pop.Professions = Pop.Professions.TRIBESPEOPLE) -> Pop:
	currentBatchNum += 1
	if (currentBatchNum > 12):
		currentBatchNum = 1
	# Creates a new pop
	var pop : Pop = Pop.new()
	
	# Adds pop to region
	pop.region = region
	region.pops.append(pop)
	
	# Changes population
	pop.batchID = currentBatchNum
	pop.changeWorkforce(workforce)
	pop.changeDependents(dependents)
	
	# Adds culture
	culture.addPop(pop)
	pops.append(pop)
	
	# Adds profession
	pop.profession = Pop.Professions.TRIBESPEOPLE
	
	# Adds tech
	var popTech : Tech = Tech.new()
	popTech.industryLevel = tech.industryLevel
	popTech.militaryLevel = tech.militaryLevel
	popTech.societyLevel = tech.societyLevel
	pop.tech = popTech
	
	return pop

func _on_tick() -> void:
	mapUpdate = false
	popTaskId = WorkerThreadPool.add_group_task(updateRegion, regions.size(), -1, false, "Updates all the regions in the game")
	#worldPopulation = worldDependents + worldWorkforce
	if (mapUpdate):
		regionSprite.texture = ImageTexture.create_from_image(regionImage)

func getRegion(pos : Vector2i) -> Object:
	var index : int = (pos.y * worldSize.x) + pos.x
	return regions[index]

#region Regions
func updateRegion(index : int) -> void:
	var region : Region = regions[index]
	region.growPops()
#endregion

#region Pops

func changePopulation(workforceIncrease : int, dependentIncrease : int) -> void:
	worldWorkforce += workforceIncrease
	worldDependents += dependentIncrease
	worldPopulation = worldWorkforce + worldDependents

#endregion

#region Cultures
func createCulture(region : Object) -> Culture:
	var newCulture : Culture = Culture.new()
	newCulture.name = "New Culture"
	var r : float = inverse_lerp(0, worldSize.x, region.pos.x)
	var g : float= inverse_lerp(0, worldSize.y, region.pos.y)
	var b : float = inverse_lerp(0, worldSize.y, region.pos.y - worldSize.x)
	newCulture.color = Color(r, g, b, 0)
	cultures.append(newCulture)
	
	return newCulture
#endregion

#region MapEdit
func setRegionColor(pos : Vector2i, color : Color) -> void:
	regionImage.set_pixel(pos.x, pos.y, Color(color, 1))
	mapUpdate = true
#endregion

func _exit_tree() -> void:
	pass
