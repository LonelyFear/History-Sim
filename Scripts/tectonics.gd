extends Node
class_name Tectonics

@export var map : UpdateTileMapLayer
@export var oceanDepth : float = 0.2
@export var seaLevel : float = 0.6

var plateTarget

enum CrustType{
	OCEANIC,
	CONTINENTAL
}

class Plate:
	var velChanged : bool
	var density : int
	var color : Color
	var dir : Vector2
	var diagDir : Vector2i
	var moveStep : Vector2i
	var tiles : Array = [Crust]

class WorldTile:
	var topCrust : Crust
	var lastPlate : Plate
	var crust : Array = [Crust]

class Crust:
	var moved : bool = false
	var age : int = 10
	var elevation : float = 0.5
	var lostElevation : float = 0
	var pos : Vector2i
	var plate : Plate
