using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;

public partial class ShaderTest : Node2D
{
	// Gets rendering device
	RenderingDevice rd = RenderingServer.GetRenderingDevice();
	// Loads shader
	[Export]
	RDShaderFile shader_file = GD.Load<RDShaderFile>("res://Shaders/shader_test.glsl");

	Rid shader;
    Rid pipeline;
	Rid texture;
	Rid storageBuffer;
	public override void _Ready() {
		// Compile shader
		shader = rd.ShaderCreateFromSpirV(shader_file.GetSpirV());
		// Creates pipeline
		pipeline = rd.ComputePipelineCreate(shader);

		float[] floats = [1.0f, 2.0f, 1.0f];
		byte[] inputData = new byte[floats.Length * sizeof(float)];

		Buffer.BlockCopy(floats, 0, inputData , 0, inputData .Length);

		storageBuffer = rd.StorageBufferCreate((uint)inputData.Length, inputData);

		Image image = GD.Load<Texture2D>("res://Sprites/Icons.png").GetImage();
		image.Convert(Image.Format.Rgbaf);

		RDTextureView textureView = new();

		RDTextureFormat textureFormat = new()
		{
			Width = 32,
			Height = 32,
			Format = RenderingDevice.DataFormat.R32G32B32A32Sfloat,

			UsageBits = RenderingDevice.TextureUsageBits.StorageBit | 
			RenderingDevice.TextureUsageBits.CanCopyFromBit |
			RenderingDevice.TextureUsageBits.SamplingBit
		};
		texture = rd.TextureCreate(textureFormat, textureView, [image.GetData()]);

		
	}

    public override void _Process(double delta)
    {
		
		RDUniform paramUniform = new()
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0,
		};
		paramUniform.AddId(storageBuffer);

		RDUniform imageUniform = new()
		{
			UniformType = RenderingDevice.UniformType.Image,
			Binding = 1,
		};
		imageUniform.AddId(texture);

        Rid uniformSet = rd.UniformSetCreate([paramUniform, imageUniform], shader, 0);
		long computeList = rd.ComputeListBegin();

		rd.ComputeListBindComputePipeline(computeList, pipeline);
		rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
		rd.ComputeListDispatch(computeList, 32, 32, 1);
		rd.ComputeListEnd();
		rd.FreeRid(uniformSet);

        // Displaying Image
        Texture2Drd texture_rd = new()
        {
            TextureRdRid = texture
        };
        GetNode<Sprite2D>("SampleImage").Texture = texture_rd;
    }


}
