extends Node2D
class_name WorldGenerator

var map : UpdateTileMapLayer

var biomes : Dictionary
var tileBiomes : Dictionary
var heightMap : Dictionary
var tempMap : Dictionary
var humidMap : Dictionary

var temps = [0.874, 0.765, 0.594, 0.439, 0.366, 0.124]
var humids = [0.941, 0.778, 0.507, 0.236, 0.073, 0.014, 0.002]
var terrainImage : Image
var worldCreated : bool = false

enum TempTypes {
	POLAR,
	ALPINE,
	BOREAL,
	COOL,
	WARM,
	SUBTROPICAL,
	TROPICAL,
	INVALID
}
enum HumidTypes{
	SUPER_ARID,
	PER_ARID,
	ARID,
	SEMI_ARID,
	SUB_HUMID,
	HUMID,
	PER_HUMID,
	SUPER_HUMID,
	INVALID
}

signal worldgenFinished()

## the size of the world in width and height
@export var worldSize : Vector2i = Vector2i(720, 360)
## the height threshold above which there will be land
@export var seaLevel : float = 0.6
## Whether or not a basic tectonic sim should be used
@export var useTectonics : bool = true
@export_category("Noise Settings")
## the seed the world generator uses
@export var seed : int
## the scale of the world generator's noise
@export var mapScale : float = 1
## Number of fractal octaves used in heightmap generation
@export var heightOctaves : float = 8
@export_category("Rivers")
## the height threshold for an area to be a river source relative to sea level
@export var riverThreshold : float = 0.4
## the amount of rivers that the world generator will attempt to generate
@export var riverCount : int

func _ready() -> void:
	
	map = $"Terrain Map"
	scale = (Vector2(1,1) * (72/float(worldSize.x)))
	map.scale = Vector2(1,1) * 16/map.tile_set.tile_size.x
	generateWorld()

#region Noise

func createHeightMap(scale : float) -> Dictionary:
	var tectonicHeightMap : Dictionary
	if (useTectonics):
		var tectonicStartTime = Time.get_ticks_msec()
		print("Tectonics simulation started")
		tectonicHeightMap = $"Tectonics".runSimulation(worldSize, Vector2i(5,4))
		print("Tectonics finished after " + str(Time.get_ticks_msec() - tectonicStartTime) + " ms")
	# Generates a heightmap with random noise
	var noiseMap = {}
	var falloff = Falloff.generateFalloff(worldSize.x, worldSize.y, 9.2, true)
	
	var simplexNoise : FastNoiseLite = FastNoiseLite.new()
	simplexNoise.fractal_octaves = heightOctaves
	simplexNoise.seed = seed
	simplexNoise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	
	# Worley noise for use later
	var noise : FastNoiseLite = FastNoiseLite.new()
	noise.fractal_octaves = 2
	noise.noise_type = FastNoiseLite.TYPE_CELLULAR
	
	# Gets our height values
	for x in worldSize.x:
		for y in worldSize.y:
			var noiseValue = inverse_lerp(-1, 1, simplexNoise.get_noise_2d(x/scale ,y/scale))
			if (useTectonics):
				noiseValue = inverse_lerp(-1, 1, simplexNoise.get_noise_2d(x/(scale/1.5) ,y/(scale/1.5)))
				noiseMap[Vector2i(x,y)] = lerpf(tectonicHeightMap[Vector2i(x,y)], noiseValue, 0.5)
			else:
				noiseMap[Vector2i(x,y)] = noiseValue - falloff[Vector2i(x, y)]
			
	# Returns the heightmap
	return noiseMap

func createTempMap(scale : float) -> Dictionary:
	# Generates a tempmap with random noise
	# Creates a random number generator for getting our seed
	var rng = RandomNumberGenerator.new()
	rng.seed = seed
	var tempMap = {}
	var noise : FastNoiseLite = FastNoiseLite.new()
	
	# Creates our noise generator
	var falloff = Falloff.generateFalloff(worldSize.x, worldSize.y, 1, false, 1.25)
	noise.fractal_octaves = 8
	noise.noise_type = FastNoiseLite.TYPE_PERLIN
	noise.seed = rand_from_seed(seed * 2)[0]
	
	# Iterates through noise
	for x in worldSize.x:
		for y in worldSize.y:
			# Gets out noise value
			var noiseValue = inverse_lerp(-1, 1, noise.get_noise_2d(x / scale ,y / scale))
			# Multiplies noise value by falloff
			tempMap[Vector2i(x,y)] = lerpf((1.0 - falloff[Vector2i(x,y)]), noiseValue, 0.15)
			# Gets our height factor, higher relative altitude = cooler temperature
			var heightFactor = (heightMap[Vector2i(x,y)] - seaLevel - 0.2)/(1 - seaLevel - 0.2)
			# Modifies temperature by height factor
			if (heightFactor > 0):
				tempMap[Vector2i(x,y)] -= heightFactor
			# Clamps temperature
			tempMap[Vector2i(x,y)] = clampf(tempMap[Vector2i(x,y)], 0, 1)
	# Returns our tempmap
	return tempMap

func createMoistMap(scale : float) -> Dictionary:
	# Generates our moisture map
	var moistMap = {}
	var noise : FastNoiseLite = FastNoiseLite.new()
	# Creates a random noise generator with a seed derived from world seed
	noise.fractal_octaves = 8
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	noise.seed = rand_from_seed(rand_from_seed(seed)[0])[0]
	# Assigns values to the map
	for x in worldSize.x:
		for y in worldSize.y:
			# Gets a lerped noise value so temperature extremes of 0 and 1 can exist
			var noiseValue = inverse_lerp(0.3, 0.7, (noise.get_noise_2d(x / scale ,y / scale) + 1)/2)
			moistMap[Vector2i(x,y)] = noiseValue
			# TODO: modify moisture map by temperature so cooler areas are less moist
	return moistMap
#endregion

func generateWorld():
	var worldGenStartTime = Time.get_ticks_msec()
	print("World generation started")
	clearMap()
	print("Generating heightmap...")
	
	var startTime = Time.get_ticks_msec()
	heightMap = createHeightMap(mapScale)
	print("Heightmap generation complete! Process took " + str(Time.get_ticks_msec() - startTime) + "ms")
	
	print("Generating tempmap...")
	startTime = Time.get_ticks_msec()
	tempMap = createTempMap(mapScale/4)
	print("Teightmap generation complete! Process took " + str(Time.get_ticks_msec() - startTime) + "ms")
	
	print("Generating moisture...")
	startTime = Time.get_ticks_msec()
	humidMap = createMoistMap(mapScale)
	print("Moisture generation complete! Process took " + str(Time.get_ticks_msec() - startTime) + "ms")
	
	print("Generating biomes...")
	startTime = Time.get_ticks_msec()
	for x in worldSize.x:
		for y in worldSize.y:
			
			var currentPos : Vector2i = Vector2i(x,y)
			FastNoiseLite.new()
			biomes[Vector2i(x,y)] = setBiome(x,y)
	print("Biome generation complete! Process took " + str(Time.get_ticks_msec() - startTime) + "ms")
	#print("Generating rivers...")
	#startTime = Time.get_ticks_msec()
	#generateRivers()
	#print("River generation complete! Process took " + str(Time.get_ticks_msec() - startTime) + "ms")
	
	print("Coloring tiles...")
	startTime = Time.get_ticks_msec()
	terrainImage = Image.create(worldSize.x, worldSize.y, true, Image.FORMAT_RGB8)
	for x in worldSize.x:
		for y in worldSize.y:
			var currentPos : Vector2i = Vector2i(x,y)
			for biome in BiomeLoader.biomes:
				if (biome["mergedIds"].has(biomes[Vector2i(x,y)])):
					map.set_cell(currentPos, 0, Vector2i(biome["textureX"],biome["textureY"]))
					terrainImage.set_pixel(x,y, biome["color"])
					#terrainImage.set_pixel(x,y, lerp(Color.BLACK, Color.WHITE, heightMap[Vector2i(x,y)]))
					tileBiomes[Vector2i(x,y)] = biome
					break
			#map.update_tile_color(currentPos, lerp(Color.BLUE, Color.RED, (heightMap[Vector2i(x,y)] - seaLevel)/(1 - seaLevel)))
			#map.get_cell_tile_data(currentpos).modulate = Color(randf_range(0, 1), randf_range(0, 1), randf_range(0, 1), 1)
	print("Tiles colored! Process took " + str(Time.get_ticks_msec() - startTime) + "ms")
	print("World generation completed after " + str(Time.get_ticks_msec() - worldGenStartTime) + "ms")
	worldCreated = true
	worldgenFinished.emit()
func clearMap():
	for i in map.get_used_cells():
		map.erase_cell(i)
	print("Tilemap clear")

#region Biomes
func setBiome(x : int, y : int) -> String:
	var altitude = heightMap[Vector2i(x,y)]
	var biome : String = "rock"
	# If we are below the ocean threshold
	if (altitude <= seaLevel):
		
		match(getTempType(x, y)):
			TempTypes.POLAR:
				biome = "polar ice"
			_:
				biome = "shallow ocean"
				if (altitude <= seaLevel - 0.1):
					biome = "ocean"
	else:
		#landTiles++;
		match (getTempType(x, y)):
			TempTypes.POLAR:
				match (getHumidType(x, y)):
					HumidTypes.SUPER_ARID:
						biome = "polar desert"
					_:
						biome = "polar ice"
			TempTypes.ALPINE:
				match(getHumidType(x, y)):
					HumidTypes.SUPER_ARID:
						biome = "subpolar dry tundra"
					HumidTypes.PER_ARID:
						biome = "subpolar moist tundra"
					HumidTypes.ARID:
						biome = "subpolar wet tundra"
					_:
						biome = "subpolar rain tundra"
			TempTypes.BOREAL:
				match (getHumidType(x, y)):
					HumidTypes.SUPER_ARID:
						biome = "boreal desert"
					HumidTypes.PER_ARID:
						biome = "boreal dry scrub"
					HumidTypes.ARID:
						biome = "boreal moist forest"
					HumidTypes.SEMI_ARID:
						biome = "boreal wet forest"
					_:
						biome = "boreal rain forest"
			TempTypes.COOL:
				match(getHumidType(x, y)):
					HumidTypes.SUPER_ARID:
						biome = "cool temperate desert"
					HumidTypes.PER_ARID:
						biome = "cool temperate desert scrub"
					HumidTypes.ARID:
						biome = "cool temperate steppe"
					HumidTypes.SEMI_ARID:
						biome = "cool temperate moist forest"
					HumidTypes.SUB_HUMID:
						biome = "cool temperate wet forest"
					_:
						biome = "cool temperate rain forest"
			TempTypes.WARM:
				match(getHumidType(x, y)):
					HumidTypes.SUPER_ARID:
						biome = "warm temperate desert"
					HumidTypes.PER_ARID:
						biome = "warm temperate desert scrub"
					HumidTypes.ARID:
						biome = "warm temperate thorn scrub"
					HumidTypes.SEMI_ARID:
						biome = "warm temperate dry forest"
					HumidTypes.SUB_HUMID:
						biome = "warm temperate moist forest"
					HumidTypes.HUMID:
						biome = "warm temperate wet forest"
					_:
						biome = "warm temperate rain forest"
			TempTypes.SUBTROPICAL:
				match(getHumidType(x, y)):
					HumidTypes.SUPER_ARID:
						biome = "subtropical desert"
					HumidTypes.PER_ARID:
						biome = "subtropical desert scrub"
					HumidTypes.ARID:
						biome = "subtropical thorn woodland"
					HumidTypes.SEMI_ARID:
						biome = "subtropical dry forest"
					HumidTypes.SUB_HUMID:
						biome = "subtropical moist forest"
					HumidTypes.HUMID:
						biome = "subtropical wet forest"
					_:
						biome = "subtropical rain forest"
			TempTypes.TROPICAL:
				match(getHumidType(x, y)):
					HumidTypes.SUPER_ARID:
						biome = "tropical desert"
					HumidTypes.PER_ARID:
						biome = "tropical desert scrub"
					HumidTypes.ARID:
						biome = "tropical thorn woodland"
					HumidTypes.SEMI_ARID:
						biome = "tropical very dry forest"
					HumidTypes.SUB_HUMID:
						biome = "tropical dry forest"
					HumidTypes.HUMID:
						biome = "tropical moist forest"
					HumidTypes.PER_HUMID:
						biome = "tropical wet forest"
					_:
						biome = "tropical rain forest"
			_:
				biome = "rock"
	return biome

func getTempType(x : int, y : int) -> TempTypes:
	var temp : float = tempMap[Vector2i(x,y)]
	if (temp < temps[5]):
		return TempTypes.POLAR
	elif (temp >= temps[5] && temp < temps[4]):
		return TempTypes.ALPINE;
	elif (temp >= temps[4] && temp < temps[3]):
		return TempTypes.BOREAL;
	elif (temp >= temps[3] && temp < temps[2]):
		return TempTypes.COOL;
	elif (temp >= temps[2] && temp < temps[1]):
		return TempTypes.WARM;
	elif (temp >= temps[1] && temp < temps[0]):
		return TempTypes.SUBTROPICAL;
	elif (temp >= temps[0]):
		return TempTypes.TROPICAL;
	else:
		return TempTypes.INVALID;

func getHumidType(x : int, y : int) -> HumidTypes:
	var humid : float = humidMap[Vector2i(x, y)]
	if ( humid < humids[6]):
		return  HumidTypes.SUPER_ARID;
	elif (humid >= humids[6] && humid < humids[5]):
		return HumidTypes.PER_ARID;
	elif (humid >= humids[5] && humid < humids[4]):
		return HumidTypes.ARID;
	elif (humid >= humids[4] && humid < humids[3]):
		return HumidTypes.SEMI_ARID;
	elif (humid >= humids[3] && humid < humids[2]):
		return HumidTypes.SUB_HUMID;
	elif (humid >= humids[2] && humid < humids[1]):
		return HumidTypes.HUMID;
	elif (humid >= humids[1] && humid < humids[0]):
		return HumidTypes.PER_HUMID; 
	elif (humid >= humids[0]):
		return HumidTypes.SUPER_HUMID;
	else:
		return HumidTypes.INVALID;
#endregion
