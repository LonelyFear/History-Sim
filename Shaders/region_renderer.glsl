#[compute]
#version 450

layout(set = 0, binding = 0, std430) readonly buffer dimensionBuffer {
    int width;
    int height;
} dimensions;

layout(set = 0, binding = 1, std430) readonly buffer colorBuffer {
    vec4[] colors;
} colors;


layout(set = 0, binding = 2, rgba32f) uniform image2D image;

layout(local_size_x = 30, local_size_y = 15, local_size_z = 1) in;

void main() {
    ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(image);
    int index = (uv.y * size.x) + uv.x;

    vec4 color = colors.colors[index];

    imageStore(image, uv, color.rgba);
}