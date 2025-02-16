extends Node2D
class_name WorldGenerator

var map : TileMapLayer
var seaLevel : float = 0.6


var biomes : Dictionary
var heightMap : Dictionary
var tempMap : Dictionary
var humidMap : Dictionary

var temps = [0.874, 0.765, 0.594, 0.439, 0.366, 0.124]
var humids = [0.941, 0.778, 0.507, 0.236, 0.073, 0.014, 0.002]

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

@export var seed : int
@export var worldSize : Vector2i = Vector2i(400, 400)
func _ready() -> void:
	scale = Vector2(1,1) * (72/float(worldSize.x))
	map = $"Map"
	generateWorld()

func createHeightMap(scale : float) -> Dictionary:
	var noiseMap = {}
	var falloff = Falloff.generateFalloff(worldSize.x, worldSize.y, 7.2, true)
	
	var simplexNoise : FastNoiseLite = FastNoiseLite.new()
	simplexNoise.fractal_octaves = 8
	simplexNoise.seed = seed
	simplexNoise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	
	var noise : FastNoiseLite = FastNoiseLite.new()
	noise.fractal_octaves = 2
	noise.noise_type = FastNoiseLite.TYPE_CELLULAR
	
	var minNoise = INF
	var maxNoise = -INF
	for x in worldSize.x:
		for y in worldSize.y:
			var noiseValue = inverse_lerp(-1, 1, simplexNoise.get_noise_2d(x/scale ,y/scale))
			if (noiseValue < minNoise):
				minNoise = noiseValue
			if (noiseValue > maxNoise):
				maxNoise = noiseValue
			noiseMap[Vector2i(x,y)] = noiseValue - falloff[Vector2i(x, y)]
			#print(noiseMap[Vector2i(x,y)])
	return noiseMap

func createTempMap(scale : float) -> Dictionary:
	var rng = RandomNumberGenerator.new()
	rng.seed = seed
	var tempMap = {}
	var noise : FastNoiseLite = FastNoiseLite.new()
	var falloff = Falloff.generateFalloff(worldSize.x, worldSize.y, 1, false, 1.25)
	noise.fractal_octaves = 8
	noise.noise_type = FastNoiseLite.TYPE_PERLIN
	noise.seed = randi()
	for x in worldSize.x:
		for y in worldSize.y:
			var noiseValue = inverse_lerp(-1, 1, noise.get_noise_2d(x / scale ,y / scale))
			tempMap[Vector2i(x,y)] = lerpf((1.0 - falloff[Vector2i(x,y)]), noiseValue, 0.15)
			#tempMap[Vector2i(x,y)] = (1.0 - falloff[Vector2i(x,y)])
	return tempMap

func createMoistMap(scale : float) -> Dictionary:
	var moistMap = {}
	var noise : FastNoiseLite = FastNoiseLite.new()
	noise.fractal_octaves = 8
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	noise.seed = rand_from_seed(seed)[0]
	for x in worldSize.x:
		for y in worldSize.y:
			var noiseValue = inverse_lerp(0.3, 0.7, (noise.get_noise_2d(x / scale ,y / scale) + 1)/2)
			moistMap[Vector2i(x,y)] = noiseValue
	return moistMap

func generateWorld():
	clearMap()
	heightMap = createHeightMap(1)
	tempMap = createTempMap(0.25)
	humidMap = createMoistMap(1)
	for x in worldSize.x:
		for y in worldSize.y:
			
			var currentPos : Vector2i = Vector2i(x,y)
			map.set_cell(currentPos, 0, Vector2i(0,0))
			FastNoiseLite.new()
			biomes[Vector2i(x,y)] = setBiome(x,y)
			for biome in BiomeLoader.biomes["biomes"]:
				if (biome["mergedIds"].has(biomes[Vector2i(x,y)])):
					map.update_tile_color(currentPos, Color(biome["color"]))
					break
			#map.update_tile_color(currentPos, lerp(Color.BLUE, Color.RED, heightMap[Vector2i(x,y)]))
			#map.get_cell_tile_data(currentpos).modulate = Color(randf_range(0, 1), randf_range(0, 1), randf_range(0, 1), 1)
func clearMap():
	for i in map.get_used_cells():
		map.erase_cell(i)

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
				if (altitude <= seaLevel - 0.05):
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
