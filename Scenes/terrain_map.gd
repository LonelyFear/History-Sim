extends UpdateTileMapLayer

@export var camera : CameraController
@export var lodMap : Sprite2D
@export var regionSprite : Sprite2D
@export var highQualityMaxZoom : float = 3

@export_category("Zoom Fade")
@export var regionMapLerpZoom : float = 3
@export var regionMinOpacity : float = 0.2
@export var baseRegionOpacity : float = 0.5
var world : WorldGenerator
var showMapId : int

func _ready() -> void:
	world = get_parent()

func _process(delta: float) -> void:
	manageMaps()
	
	# Makes our region map more transparent as we zoom in
	if (camera.zoom.x >= regionMapLerpZoom && regionSprite):
		# Slowly makes region map transparent
		regionSprite.self_modulate.a = lerp(regionMinOpacity, baseRegionOpacity, clampf(inverse_lerp(10, regionMapLerpZoom, camera.zoom.x), 0, 1))
	elif (regionSprite):
		regionSprite.modulate.a = baseRegionOpacity

func manageMaps():
	if (world.worldCreated):
		# Switches between a sprite and tilemap for displaying map
		if (camera && camera.visible):
			var zoomVal = camera.zoom.x
			if zoomVal < highQualityMaxZoom && visible == true:
				hideMap()
			elif visible == false && zoomVal >= highQualityMaxZoom:
				showMapId = WorkerThreadPool.add_task(showMap)

func hideMap():
	visible = false
	lodMap.texture = ImageTexture.create_from_image(world.terrainImage)
	lodMap.visible = true

func showMap():
	lodMap.call_deferred("set_visible", false)
	call_deferred("set_visible", true)
func _exit_tree() -> void:
	pass
	#WorkerThreadPool.wait_for_task_completion(showMapId)
