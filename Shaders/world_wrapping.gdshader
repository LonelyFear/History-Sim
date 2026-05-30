shader_type canvas_item;

uniform float horizontalPos = 0;

void fragment() {
    // fract keeps the UV between 0 and 1, creating a loop
    vec2 shifted_uv = vec2(fract(UV.x + horizontalPos), UV.y);
    COLOR = texture(TEXTURE, shifted_uv);
}
