extends TileMapLayer

@export var camera : CameraController
@export var lodMap : Sprite2D
@export var regionSprite : Sprite2D
@export var highQualityMaxZoom : float = 3

@export_category("Zoom Fade")
@export var regionMapLerpZoom : float = 3
@export var regionMinOpacity : float = 0.2
@export var baseRegionOpacity : float = 0.5
var world : Node
var showMapId : int

func _ready() -> void:
	world = get_parent()
	camera = get_node("/root/Game/PlayerCamera")

func _process(_delta: float) -> void:
	manageMaps()
	# Makes our region map more transparent as we zoom in
	if (camera.zoom.x >= regionMapLerpZoom && regionSprite):
		# Slowly makes region map transparent
		regionSprite.self_modulate.a = lerp(regionMinOpacity, baseRegionOpacity, clampf(inverse_lerp(10, regionMapLerpZoom, camera.zoom.x), 0, 1))
	elif (regionSprite):
		regionSprite.self_modulate.a = baseRegionOpacity
	regionSprite.self_modulate.a = baseRegionOpacity

func manageMaps() -> void:
	if (world.worldCreated):
		# Switches between a sprite and tilemap for displaying map
		if (camera && camera.visible):
			var zoomVal : float = camera.zoom.x
			if zoomVal < highQualityMaxZoom && visible == true:
				hideMap()
			elif visible == false && zoomVal >= highQualityMaxZoom:
				showMapId = WorkerThreadPool.add_task(showMap)

func hideMap() -> void:
	visible = false
	lodMap.texture = ImageTexture.create_from_image(world.terrainImage)
	lodMap.visible = true

func showMap() -> void:
	lodMap.call_deferred("set_visible", false)
	call_deferred("set_visible", true)
