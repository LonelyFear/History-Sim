extends Node
class_name Tectonics

@export var map : UpdateTileMapLayer
@export var oceanDepth : float = 0.5
@export var seaLevel : float = 0.55
@export var worldGen : WorldGenerator

var tiles : Dictionary
var plates : Array = []

var plateTarget : int
var worldSize : Vector2i
var randomIterator : int
var seed : int

enum CrustTypes {
	OCEANIC,
	CONTINENTAL
}

func _ready() -> void:
	worldSize = worldGen.worldSize
	seed = worldGen.seed
	#createPlates(4, 4)
	#initHeightmap()
	#initTilemap()

func _process(delta: float) -> void:
	pass
	#movePlates()
	#checkTiles()
	#updateTilemap()


func movePlates():
	for plate in plates:
		# Randomize Plate Velocity
		if (randf() < 0.1):
			plate.dir += Vector2(randf_range(-.1,.1), randf_range(-.1,.1))
		# Normalized Plate Velocity
		if (plate.dir.length() > 1):
			plate.dir = plate.dir.normalized() * 1
		
		plate.diagDir = Vector2i(0,0)
		plate.moveStep += Vector2i(1, 1)
		if plate.moveStep.x > abs(1 / fmod(plate.dir.x, 1)):
			plate.moveStep.x = 0;
			plate.diagDir.x = sign(plate.dir.x)
		if plate.moveStep.y > abs(1 / fmod(plate.dir.y, 1)):
			plate.moveStep.y = 0;
			plate.diagDir.y = sign(plate.dir.y)
	# Moves plates
	for x in worldSize.x:
		for y in worldSize.y:
			var tile : WorldTile = tiles[Vector2i(x,y)]
			if (tile.topCrust == null):
				continue
			
			tile.lastPlate = tile.topCrust.plate
			var crustArray : Array
			crustArray.append_array(tile.crust)
			for crust : Crust in crustArray:
				crust.age += 1
				
				var plate : Plate = crust.plate
				var dx : int = int(plate.dir.x)
				var dy : int = int(plate.dir.y)
				dx += plate.diagDir.x
				dy += plate.diagDir.y
				
				var newPos : Vector2i = getNewPos(Vector2i(x,y), Vector2i(dx,dy))
				var newTile : WorldTile = tiles[Vector2i(newPos)]
				if (!crust.moved):
					tile.crust.erase(crust)
					crust.pos = newPos
					newTile.crust.append(crust)
					crust.moved = true

func checkTiles():
	var rng = RandomNumberGenerator.new()
	for x in worldSize.x:
		for y in worldSize.y:
			var pos = Vector2i(x,y)
			var tile : WorldTile = tiles[Vector2i(x,y)]
			var crustArray : Array
			crustArray.append_array(tile.crust)
			for crust : Crust in crustArray:
				crust.moved = false
				if (crust.age < 5):
					lerpf(seaLevel - oceanDepth, crust.elevation, 0.75)
			# Divergence
			if (tile.crust.size() < 1):
				var newCrust : Crust = Crust.new()
				newCrust.plate = tile.lastPlate
				newCrust.pos = Vector2i(x,y)
				newCrust.age = 0
				newCrust.elevation = seaLevel - oceanDepth + randfn(0.1, 0.05)
				tile.crust.append(newCrust)
				tile.lastPlate.crust.append(newCrust)
			tile.topCrust = tile.crust[0]
			# Convergence
			if (tile.crust.size() > 0):
				var topCrust : Crust = null
				var lowestAge = INF
				
				for crust in tile.crust:
					var continentalFactor = 0
					if (crust.crustType == CrustTypes.CONTINENTAL):
						continentalFactor = 10000
					var ageModified = crust.age + crust.plate.density - continentalFactor
					if ageModified < lowestAge:
						lowestAge = ageModified
						topCrust = crust
				tile.topCrust = topCrust
				
				for crust : Crust in tile.crust:
					if crust != tile.topCrust:
						if crust.crustType == CrustTypes.OCEANIC:
							crust.lostElevation += randf_range(0.05, 0.1)
							if (crust.lostElevation > crust.elevation):
								topCrust.elevation += randf_range(0.01, 0.02)
								DeleteCrust(pos, crust)
						else:
							crust.lostElevation += randf_range(0.025, 0.05)
							topCrust.elevation += 0.01
							if (crust.lostElevation > crust.elevation):
								DeleteCrust(pos, crust)
				for crust : Crust in tile.crust:
					if crust.elevation > 1:
						crust.elevation = 1

func initTilemap():
	if (map):
		for x in worldSize.x:
			for y in worldSize.y:
				var currentPos : Vector2i = Vector2i(x,y)
				map.set_cell(currentPos, 0, Vector2i(0,0))

func updateTilemap():
	for x in worldSize.x:
		for y in worldSize.y:
			var color : Color
			var tile : WorldTile = tiles[Vector2i(x,y)]
			if (tile.topCrust != null):
				color = lerp(Color.BLACK, Color.WHITE, tile.topCrust.elevation)
				if (tile.topCrust.elevation > seaLevel):
					color = lerp(Color.SEA_GREEN, Color.DARK_SLATE_GRAY, (tile.topCrust.elevation - seaLevel)/(1 - seaLevel))
				else:
					color = lerp(Color.DARK_BLUE, Color.DEEP_SKY_BLUE, tile.topCrust.elevation + oceanDepth)
				#color = tile.topCrust.plate.color
			map.update_tile_color(Vector2i(x,y), color)
		

func DeleteCrust(pos : Vector2i, crust : Crust):
	var tile : WorldTile = tiles[pos]
	tile.crust.erase(crust)
	crust.plate.crust.erase(crust)

func getNewPos(pos : Vector2i, dir : Vector2i) -> Vector2i:
	var newPos = pos + dir
	if (newPos.x >= worldSize.x):
		newPos.x = 0
	if (newPos.x < 0):
		newPos.x = worldSize.x - 1
	
	if (newPos.y >= worldSize.y):
		newPos.y = 0
	if (newPos.y < 0):
		newPos.y = worldSize.y - 1
	return newPos

func createPlates(gridSizeX : int, gridSizeY : int):
	for x in worldSize.x:
		for y in worldSize.y:
			tiles[Vector2i(x,y)] = WorldTile.new()
	var densities : Array = []
	
	for i in range(gridSizeX * gridSizeY):
		densities.append(i)
	
	var ppcX : int = worldSize.x/gridSizeX
	var ppcY : int = worldSize.y/gridSizeY
	
	var points : Dictionary
	var plateOrigins : Dictionary
	# Makes plates
	for gx in gridSizeX:
		for gy in gridSizeY:
			
			var x = gx * ppcX + randi_range(0, ppcX)
			var y = gy * ppcY + randi_range(0, ppcY)
			
			points[Vector2i(gx,gy)] = Vector2i(x, y)
			
			var newPlate : Plate = Plate.new()
			newPlate.color = Color(randf(), randf(), randf())
			newPlate.dir = Vector2(randi_range(-2, 2), randi_range(-2, 2))
			
			var di = randi_range(0, densities.size() - 1)
			newPlate.density = densities[di]
			densities.remove_at(di)
			
			plates.append(newPlate)
			plateOrigins[points[Vector2i(gx,gy)]] = newPlate
			map.update_tile_color(points[Vector2i(gx,gy)], newPlate.color)
	
	for x in worldSize.x:
		for y in worldSize.y:
			var tile : WorldTile = tiles[Vector2i(x,y)]
			var closestPlate : Plate
			var nearestPoint : Vector2i
			var closestDist  : float = INF
			
			for dx in range(-1,1):
				for dy in range(-1,1):
					var gx : int = x / ppcX
					var gy : int = y / ppcY
					
					var tx : int = gx + dx
					var ty : int = gy + dy
					
					if (tx < 0):
						tx = gridSizeX - 1
					if (tx >= gridSizeX):
						tx = 0
					
					if (ty < 0):
						ty = gridSizeY - 1
					if (ty >= gridSizeY):
						ty = 0
					randomIterator += 1
					var dist = getWrappedDist(Vector2i(x, y), points[Vector2i(tx, ty)])
					if (dist < closestDist):
						closestDist = dist
						nearestPoint = points[Vector2i(tx, ty)]
			
			closestPlate = plateOrigins[nearestPoint]
			var newCrust : Crust = Crust.new()
			newCrust.plate = closestPlate
			newCrust.pos = Vector2i(x,y)
			
			tile.crust.append(newCrust)
			tile.topCrust = newCrust
			closestPlate.crust.append(newCrust)

func initHeightmap():
	var noise : FastNoiseLite = FastNoiseLite.new()
	noise.fractal_octaves = 8
	noise.seed = seed
	noise.TYPE_VALUE
	
	var falloffMap = Falloff.generateFalloff(worldSize.x, worldSize.y, 7.2, true)
	for x in worldSize.x:
		for y in worldSize.y:
			var height = clampf((noise.get_noise_2d(x,y) + 1)/2 - falloffMap[Vector2i(x,y)], 0, 1)
			var tile : WorldTile = tiles[Vector2i(x,y)]
			
			
			if height > seaLevel:
				tile.topCrust.crustType = CrustTypes.CONTINENTAL
			else:
				height = lerpf(seaLevel - oceanDepth, seaLevel, calcInverseFalloff(inverse_lerp(seaLevel, 0, height)))
			tile.topCrust.elevation = height
	
func calcInverseFalloff(v : float):
	var a = 3
	var b = .15
	return 1 - pow(v, a) / (pow(v, a) + pow(b - b*v, a))

func getWrappedDist(a : Vector2, b : Vector2) -> float:
	var noise : FastNoiseLite = FastNoiseLite.new()
	noise.fractal_octaves = 8
	noise.seed = rand_from_seed(seed)[0]
	noise.TYPE_VALUE
	
	a += Vector2(noise.get_noise_1d(randomIterator/80) * 6, noise.get_noise_1d(randomIterator/80) * 6)
	#b += Vector2(noise.get_noise_1d((randomIterator-499)/80) * 4, noise.get_noise_1d((randomIterator-6671)/80) * 4)
	
	var dx = abs(a.x - b.x) 
	if (dx > worldSize.x/2):
		dx = worldSize.x - dx
	dx += (noise.get_noise_1d(randomIterator/80) * 4)
	var dy = abs(a.y - b.y)
	if (dy > worldSize.y/2):
		dy = worldSize.y - dy
	

	## Manhattan
	return dx + dy
	## Eucledian
	return sqrt(pow(dx,2) + pow(dy,2))

class Plate:
	var velChanged : bool
	var density : int
	var color : Color
	var dir : Vector2
	var diagDir : Vector2i
	var moveStep : Vector2i
	var crust : Array = []

class WorldTile:
	var topCrust : Crust
	var lastPlate : Plate
	var crust : Array = []

class Crust:
	var moved : bool = false
	var age : int = 10
	var elevation : float = 0
	var lostElevation : float = 0
	var pos : Vector2i
	var plate : Plate
	var crustType : CrustTypes = CrustTypes.OCEANIC
