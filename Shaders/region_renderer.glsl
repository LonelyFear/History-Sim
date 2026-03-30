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

int getIndex(int posX, int posY){
    return ((posY % dimensions.height) * dimensions.width) + (posX % dimensions.width);
}

void main() {
    ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(image);
    int index = getIndex(uv.x, uv.y);

    vec4 color = colors.colors[index];
    if (colors.colors[getIndex(uv.x + 1, uv.y + 1)] != color){
        color = vec4(0,0,0,1);
    }

    imageStore(image, uv, color.rgba);
}

