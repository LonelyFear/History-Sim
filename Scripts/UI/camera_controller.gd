extends Camera2D
class_name CameraController

@export var cameraSpeed : float = 500.0
@export var controlEnabled : bool = false

var positionTarget : Vector2

var zoomTarget : Vector2
var zoomSpeed : float = 1
var minZoom : float = 6
var maxZoom : float = 1

var dragging : bool
var draggingStartMousePos : Vector2
var draggingStartPos : Vector2

func _ready() -> void:
	positionTarget = position

func _process(delta: float) -> void:
	if (controlEnabled):
		zoomCamera(delta)
		keyboardPan(delta)
		dragPan()

func keyboardPan(delta : float) -> void:
	var movementVector : Vector2
	movementVector.x = Input.get_axis("Move_Left", "Move_Right")
	movementVector.y = Input.get_axis("Move_Up", "Move_Down")
	
	position += (movementVector * cameraSpeed * delta * (1/zoom.x))

func dragPan() -> void:
	if (!dragging && Input.is_action_just_pressed("Camera_Pan")):
		draggingStartMousePos = get_viewport().get_mouse_position()
		draggingStartPos = position
		dragging = true
	
	if (dragging && Input.is_action_just_released("Camera_Pan")):
		dragging = false
	
	if (dragging):
		var moveVector : Vector2 = get_viewport().get_mouse_position() - draggingStartMousePos
		position = draggingStartPos - moveVector * (1/zoom.x)

func zoomCamera(delta : float) -> void:
	if (Input.is_action_just_released("Zoom_In")):
		zoomTarget *= 1.1
	elif (Input.is_action_just_released("Zoom_Out")):
		zoomTarget *= 0.9
	zoomTarget = clamp(zoomTarget, Vector2(maxZoom, maxZoom), Vector2(minZoom, minZoom))
	
	var oldZoom = zoom.x
	zoom = lerp(zoom, zoomTarget, zoomSpeed * delta)
	
	# Zooming on position
	var zoomDir : Vector2 = get_global_mouse_position() - position
	position += zoomDir - (zoomDir / (zoom.x/oldZoom))
