extends Node
class_name BiomeLoader
@export var biomeDataPath : String = "res://Json Resources/biomes.json"
static var biomes : Dictionary

func _init() -> void:
	if (FileAccess.file_exists(biomeDataPath)):
		var biomesFile = FileAccess.open(biomeDataPath, FileAccess.READ)
		biomes = JSON.parse_string(biomesFile.get_as_text())
		print(biomes["biomes"][1])
