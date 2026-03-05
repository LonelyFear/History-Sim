#[compute]
#version 450

layout(set = 0, binding = 0, std430) readonly buffer parameters {
    float r;
    float g;
    float b;
} params;

layout(set = 0, binding = 1, rgba32f) uniform image2D image;

layout(local_size_x = 1, local_size_x = 1, local_size_z = 1) in;

void main() {
    ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
    vec4 color = imageLoad(image, uv);
    //float average = ;
    vec3 finalColor = vec3(0, 0, 0);

    if (vec3(color) != vec3(0,0,0)){
        finalColor = vec3(uv.x/32.0, uv.y/32.0, 0);
    } 


    imageStore(image, uv, vec4(finalColor, color.a));
}