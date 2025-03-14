extends Node
class_name Falloff

static func generateFalloff(width : int, height : int, b : float, includeX : bool, a : float = 3) -> Dictionary:
	var map : Dictionary = {}
	for i in width:
		for j in width:
			var x : float = clamp(i / float(width) * 2 - 1, -1, 1)
			var y : float = clamp(j / float(height) * 2 - 1, -1, 1)
			var val : float = abs(y)
			if (includeX):
				val = max(abs(x), abs(y))
			map[Vector2i(i,j)] = evaluate(val, b, a)
	return map

static func evaluate(value : float, b : float = 7.2, a : float = 3) -> float:
	return pow(value, a) / (pow(value, a) + pow(b - (b * value), a));
