shader_type canvas_item;

uniform sampler2D noiseTexture : repeat_enable;
uniform sampler2D screenTexture : hint_screen_texture, filter_nearest;
void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	float offset = texture(noiseTexture,UV + TIME/5.0).r;
	vec4 screenColor = texture(screenTexture,SCREEN_UV + offset * 0.03);
	float uvClip = (sin(UV.x*2.0 + TIME + offset)) * 0.04;
	
	if (UV.y - uvClip > 0.04)
		COLOR = screenColor * vec4(0.9, 0.9, 1.3, 1);
	else
		COLOR = vec4(0);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
