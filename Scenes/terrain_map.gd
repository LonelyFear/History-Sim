extends UpdateTileMapLayer

@export var camera : CameraController
@export var lodMap : Sprite2D
@export var regionMap : TileMapLayer
@export var highQualityMaxZoom : float = 3

@export_category("Zoom Fade")
@export var regionMapLerpZoom : float = 3
@export var regionMinOpacity : float = 0.2
@export var baseRegionOpacity : float = 0.5
var world : WorldGenerator

var mapThread : Thread = Thread.new()

func _ready() -> void:
	world = get_parent()

func _process(delta: float) -> void:
	manageMaps()
	
	# Makes our region map more transparent as we zoom in
	if (camera.zoom.x >= regionMapLerpZoom && regionMap):
		# Slowly makes region map transparent
		regionMap.self_modulate.a = lerp(regionMinOpacity, baseRegionOpacity, clampf(inverse_lerp(10, regionMapLerpZoom, camera.zoom.x), 0, 1))
	elif (regionMap):
		regionMap.modulate.a = baseRegionOpacity

func manageMaps():
	if (world.worldCreated):
		# Switches between a sprite and tilemap for displaying map
		if (camera && camera.visible):
			var zoomVal = camera.zoom.x
			if zoomVal < highQualityMaxZoom && visible == true:
				hideMap()
			elif visible == false && zoomVal >= highQualityMaxZoom:
				mapThread.start(showMap)


func hideMap():
	visible = false
	lodMap.texture = ImageTexture.create_from_image(world.terrainImage)
	lodMap.visible = true

func showMap():
	lodMap.visible = false
	visible = true

func _exit_tree() -> void:
	mapThread.wait_to_finish()
