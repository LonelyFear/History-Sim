extends Node

var modsPath = "res://Mods/"
var validModPaths = []
var modids = []

func _ready() -> void:
	pass
	#loadMods(modsPath)
	#print("Mod loading complete: Succesfully loaded " + str(validModPaths.size()) + " mods")

func loadMods(path : String):
	if DirAccess.dir_exists_absolute(path):
		print("Found " + str(DirAccess.get_directories_at(path).size()) + " directories")
		for mod in DirAccess.get_directories_at(path):
			var modPath = modsPath + mod
			if DirAccess.dir_exists_absolute(modPath):
				var modJsonPath = modPath + "/mod.json"
				if (FileAccess.file_exists(modJsonPath)):
					var modJsonFile = FileAccess.open(modJsonPath, FileAccess.READ)
					var modData : Dictionary = JSON.parse_string(modJsonFile.get_as_text())
					
					if modData is Dictionary and modData.has("author") and modData.has("version") and modData.has("modid") and modData.has("name"):
						if (!modids.has(modData["modid"])):
							print("Succesfully loaded " + modData["name"] + " by " + modData["author"])
							print("Version: " + str(modData["version"]))
							if modData.has("description"):
								print("Description: " + modData["description"])
							
							validModPaths.append(modPath)
							modids.append(modData["modid"])
						else:
							print("Mod id already in use!")
					else:
						print("Error loading mod " + mod + ": mod.json invalid")
				else:
					print("Error loading mod " + mod + ": mod.json not found")
			else:
				print("Error loading mod " + mod + ": Path invalid")
	else:
		print("Error loading mods: mods directory invalid")
