#[compute]

#version 450
#extension GL_ARB_gpu_shader_int64 : enable

layout(set = 0, binding = 0, std430) readonly buffer dimensionBuffer {
    int width;
    int height;
    int resolution;
} dimensions;

layout(set = 0, binding = 1, std430) readonly buffer colorBuffer {
    vec4[] colors;
} colors;

layout(set = 0, binding = 2, rgba32f) uniform image2D image;

layout(set = 0, binding = 3, std430) readonly buffer borderBuffer {
    uint64_t[] borders;
} borderData;

layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

int getIndex(int posX, int posY){
    ivec2 size = ivec2(dimensions.width, dimensions.height);
    return ((posY % size.y) * size.x) + (posX % size.x);
}

void main() {
    ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(image);
    
    int index = getIndex(uv.x/dimensions.resolution, uv.y/dimensions.resolution);
    vec4 color = colors.colors[index];
    
    int borderIndex = 0;
    bool isBorderToRender = false;

    ivec2[] positionsToCheck = ivec2[4](ivec2(uv.x, uv.y + 1), ivec2(uv.x, uv.y - 1), ivec2(uv.x + 1, uv.y), ivec2(uv.x - 1, uv.y));

    for (int i = 0; i < positionsToCheck.length(); i++){
        ivec2 pos = positionsToCheck[i];
        if (borderData.borders[getIndex(pos.x/dimensions.resolution, pos.y/dimensions.resolution)] != borderData.borders[index]){
            isBorderToRender = true;
            break;
        }
    }


    if (isBorderToRender){
        color = vec4(0,0,0,1);
    }
    
    imageStore(image, uv, color.rgba);
}

