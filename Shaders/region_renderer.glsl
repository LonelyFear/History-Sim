#[compute]

#version 450
#extension GL_ARB_gpu_shader_int64 : enable

layout(set = 0, binding = 0, std430) readonly buffer dimensionBuffer {
    float width;
    float height;
    float resolution;
    float opacity;
    float thinBorderThickness;
    float solidBorderThickness;
} dimensions;

layout(set = 0, binding = 1, std430) readonly buffer colorBuffer {
    vec4[] colors;
} colors;

layout(set = 0, binding = 2, rgba32f) uniform image2D image;

layout(set = 0, binding = 3, std430) readonly buffer borderBuffer {
    ivec2[] borders;
} borderData;

layout(set = 0, binding = 4, std430) readonly buffer cameraBuffer {
    float zoom;
} camera;

layout(set = 0, binding = 5, rgba32f) uniform image2D terrainImage;

layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

int getIndex(int posX, int posY){
    ivec2 size = ivec2(int(dimensions.width), int(dimensions.height));
    return ((posY % size.y) * size.x) + (posX % size.x);
}

void main() {
    ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(image);
    
    int index = getIndex(uv.x/int(dimensions.resolution), uv.y/int(dimensions.resolution));

    vec4 terrainColor = imageLoad(terrainImage, uv/int(dimensions.resolution));
    vec4 color = colors.colors[index];
    
    int borderId = borderData.borders[index].x;
    int borderTag = borderData.borders[index].y;
    bool isBorderToRender = false;
    bool isBorderSolid = false;

    int thickness = 1;

    for (int dx = -thickness; dx <= thickness; dx++){
        for (int dy = -thickness; dy <= thickness; dy++){
            if (dx != 0 && dy != 0){
                ivec2 pos = ivec2((uv.x + dx) % int(dimensions.width), (uv.y + dy) % int(dimensions.height));
                int otherIndex = getIndex(pos.x/int(dimensions.resolution), pos.y/int(dimensions.resolution));
                int otherBorderId = borderData.borders[otherIndex].x;
                int otherBorderTag = borderData.borders[otherIndex].y;
                
                if (otherBorderId != borderId){
                    isBorderToRender = true;
                    if (otherBorderTag != borderTag) isBorderSolid = true;
                }                 
            }
        }
    }


    if (isBorderToRender){
        color = vec4(color.rgb * 0.75f, 1);
        if (isBorderSolid){
            color = vec4(0,0,0,1);
        }
    } else if (color.a > 0) {
        color = (color * dimensions.opacity) + (terrainColor * (1.0 - dimensions.opacity));
    }
    
    imageStore(image, uv, color.rgba);
}

