extends Camera2D
class_name CameraController

@export var cameraSpeed : float = 100.0
@export var cameraAccel : float = 50.0


var velocity : Vector2
@export_category("Zooming")
@export var zoomSpeed : float = 10.0
@export var minZoom : Vector2 = Vector2(6, 6)
@export var maxZoom : Vector2 = Vector2(1, 1)



func _process(delta: float) -> void:
	cameraControl()

func cameraControl():
	var cameraRealSpeed = cameraSpeed / zoom.x
	var cameraRealAccel = cameraAccel / zoom.x
	var movementVector : Vector2
	movementVector.x = Input.get_axis("Move_Left", "Move_Right")
	movementVector.y = Input.get_axis("Move_Up", "Move_Down")
	
	velocity += movementVector * cameraRealAccel * get_process_delta_time()
	if (velocity.length() > cameraRealSpeed):
		velocity = velocity.normalized() * cameraRealSpeed
	velocity *= 0.9
	
	position += velocity
	
	var zoomVel = int(Input.is_action_just_released("Zoom_In")) - int(Input.is_action_just_released("Zoom_Out"))
	zoom += Vector2(zoomVel, zoomVel) * (zoomSpeed * get_process_delta_time())
	zoom = clamp(zoom, maxZoom, minZoom)
