extends TileMapLayer
class_name UpdateTileMapLayer

var _update_fn : Dictionary = {} # of Dictionary of Callable

func update_tile_color(coords: Vector2i, color : Color) -> void:
	update_tile(coords, func (tile_data:TileData) -> void: tile_data.modulate = color )

func update_tile(coords: Vector2i, fn: Callable) -> void:
	_update_fn[coords] = fn
	notify_runtime_tile_data_update()

func _use_tile_data_runtime_update(coords: Vector2i) -> bool:
	if not _update_fn.has(coords): return false
	return true

func _tile_data_runtime_update(coords: Vector2i, tile_data: TileData) -> void:
	if not _update_fn.has(coords): pass
	var fn: Callable = _update_fn[coords]
	fn.call(tile_data)
	_update_fn.erase(coords)
