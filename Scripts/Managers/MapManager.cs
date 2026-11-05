using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

[GlobalClass]
public partial class MapManager : Node2D
{
    SimManager simManager;
    [ExportCategory("References")]
    [Export] TimeManager timeManager;
    [Export] SelectionManager selectionManager;
    [Export] public Sprite2D regionOverlay;
    [Export] public Sprite2D terrainSprite;
    [Export] SimManagerHolder simHolder;
    [Export] PlayerCamera playerCamera;

    [ExportCategory("Rendering")]
    [Export] int tileResolution = 4;

    Vector2I worldSize;
    //ImageTexture regionTexture;
    Image regionImage;
    Image terrainImage;
    
    public IMapMode mapMode;
    IMapMode fallbackMapMode = new FallbackMapMode();
    public Dictionary<string, IMapMode> mapModes = [];

    public bool initialized = false;
     
    // Shaders
    RenderingDevice rd = RenderingServer.GetRenderingDevice();
    RDShaderFile regionShaderFile = GD.Load<RDShaderFile>("res://Shaders/region_renderer.glsl");
    Rid regionOverlayShader;
    Rid regionOverlayShaderPipeline;
	Rid texture;
    Rid terrainTexture;
    Texture2Drd texture_rd;
	Rid dimensionsBuffer;
    Rid colorsBuffer;
    Rid bordersBuffer;
    Rid cameraBuffer;

    // Tasks
    Task drawTask = Task.CompletedTask;

    // Input arrays
    Color[] regionColors;
    (int,int)[] borderValues;

    // Events
    public delegate void MapmodeChanged(IMapMode newMapMode);
    public event MapmodeChanged mapmodeChanged;

    // NOTE: Painted regions are updated in TimeManager.cs
    public override void _Ready()
    {
        regionOverlay = GetNode<Sprite2D>("Region Map");
		simHolder.simStartEvent += InitMapManager;
        RegisterMapMode(new PopulationMapMode());
        RegisterMapMode(new WealthMapMode());
        RegisterMapMode(new TradeZoneMapMode());
        RegisterMapMode(new TechMapMode());
        RegisterMapMode(new RealmMapMode());
        RegisterMapMode(new PolityMapMode());
        RegisterMapMode(new CultureMapMode());
        RegisterMapMode(new AllianceMapMode());
        mapMode = mapModes["realm"];
	}
    void RegisterMapMode(IMapMode mapMode)
    {
        mapModes.Add(mapMode.Id, mapMode);
    }
    void InitMapManager() {
        // Frees dimension buffer
        if (colorsBuffer.IsValid) rd.FreeRid(colorsBuffer);
        if (dimensionsBuffer.IsValid) rd.FreeRid(dimensionsBuffer);
        if (terrainTexture.IsValid) rd.FreeRid(terrainTexture);

        simManager = simHolder.simManager;
        simManager.mapManager = this;

        Scale = simManager.terrainMap.Scale/tileResolution;

        worldSize = SimManager.worldSize;
        borderValues = new (int,int)[worldSize.X * worldSize.Y];
        regionColors = new Color[worldSize.X * worldSize.Y];

        regionImage = Image.CreateEmpty(worldSize.X*tileResolution, worldSize.Y*tileResolution, false, Image.Format.Rgbaf);
        terrainImage = ((TerrainMap)simManager.terrainMap).terrainMap.Texture.GetImage();

        IMapMode.simManager = simManager;
        IMapMode.selectionManager = selectionManager;
        InitShader();
        
        initialized = true;
    }
    public void InitShader()
    {
        // Creates shader and pipeline
        regionOverlayShader = rd.ShaderCreateFromSpirV(regionShaderFile.GetSpirV());
		regionOverlayShaderPipeline = rd.ComputePipelineCreate(regionOverlayShader);

        // Creates a buffer for world size

        // Creates image buffer
        RDTextureView textureView = new();
		RDTextureFormat textureFormat = new()
		{
			Width = (uint)regionImage.GetSize().X,
			Height = (uint)regionImage.GetSize().Y,
			Format = RenderingDevice.DataFormat.R32G32B32A32Sfloat,

			UsageBits = RenderingDevice.TextureUsageBits.StorageBit | 
			RenderingDevice.TextureUsageBits.CanCopyFromBit |
			RenderingDevice.TextureUsageBits.SamplingBit
		};
        texture = rd.TextureCreate(textureFormat, textureView, [regionImage.GetData()]);  

        textureView = new();
		textureFormat = new()
		{
			Width = (uint)terrainImage.GetSize().X,
			Height = (uint)terrainImage.GetSize().Y,
			Format = RenderingDevice.DataFormat.R32G32B32A32Sfloat,

			UsageBits = RenderingDevice.TextureUsageBits.StorageBit |
			RenderingDevice.TextureUsageBits.SamplingBit
		};
        terrainTexture = rd.TextureCreate(textureFormat, textureView, [terrainImage.GetData()]);   

        // Setting up image display
        texture_rd = new()
        {
            TextureRdRid = texture,
        }; 
        regionOverlay.Texture = texture_rd;
    }
    public void UpdateBuffers()
    {    
        var partitioner = Partitioner.Create(simManager.regionIds.Values);
        Parallel.ForEach(partitioner, UpdateRegionColor);

        float[] dimensionsArray = [worldSize.X, worldSize.Y, tileResolution, mapMode.Opacity, 1, 1];
        byte[] dimensionBytes = MemoryMarshal.AsBytes(dimensionsArray.AsSpan()).ToArray();
        dimensionsBuffer= rd.StorageBufferCreate((uint)dimensionBytes.Length, dimensionBytes);  

        byte[] colorsBytes = MemoryMarshal.AsBytes(regionColors.AsSpan()).ToArray();
        colorsBuffer = rd.StorageBufferCreate((uint)colorsBytes.Length, colorsBytes);  

        byte[] borderBytes = MemoryMarshal.AsBytes(borderValues.AsSpan()).ToArray();
        bordersBuffer = rd.StorageBufferCreate((uint)borderBytes.Length, borderBytes); 

        float[] cameraData = [playerCamera.Zoom.X];

        byte[] cameraBytes = MemoryMarshal.AsBytes(cameraData.AsSpan()).ToArray();
        cameraBuffer = rd.StorageBufferCreate((uint)cameraBytes.Length, cameraBytes);             
    }
    public void RunShader()
    {
        RDUniform dimensionsUniform = new()
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0,
		};
		dimensionsUniform.AddId(dimensionsBuffer);

        RDUniform colorsUniform = new()
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 1,
		};
		colorsUniform.AddId(colorsBuffer);

		RDUniform imageUniform = new()
		{
			UniformType = RenderingDevice.UniformType.Image,
			Binding = 2,
		};
		imageUniform.AddId(texture);

        RDUniform bordersUniform = new()
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 3,
		};
		bordersUniform.AddId(bordersBuffer);

        RDUniform cameraUniform = new()
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 4,
		};
		cameraUniform.AddId(cameraBuffer);   

		RDUniform terrainUniform = new()
		{
			UniformType = RenderingDevice.UniformType.Image,
			Binding = 5,
		};
		terrainUniform.AddId(terrainTexture);     

        Rid uniformSet = rd.UniformSetCreate([dimensionsUniform, colorsUniform, imageUniform, bordersUniform, cameraUniform, terrainUniform], regionOverlayShader, 0);
		long computeList = rd.ComputeListBegin();

		rd.ComputeListBindComputePipeline(computeList, regionOverlayShaderPipeline);
		rd.ComputeListBindUniformSet(computeList, uniformSet, 0);

		rd.ComputeListDispatch(computeList, (uint)regionImage.GetSize().X/16, (uint)regionImage.GetSize().Y/16, 1);
		rd.ComputeListEnd();

		if (uniformSet.IsValid) rd.FreeRid(uniformSet);
        if (colorsBuffer.IsValid) rd.FreeRid(colorsBuffer);
        if (bordersBuffer.IsValid) rd.FreeRid(bordersBuffer);
        if (cameraBuffer.IsValid) rd.FreeRid(cameraBuffer);
        if (dimensionsBuffer.IsValid) rd.FreeRid(dimensionsBuffer);
    }
    public override void _Process(double delta)
    {
        if (initialized)
        {
            ((ShaderMaterial)regionOverlay.Material).SetShaderParameter("horizontalPos", playerCamera.HorizontalFactor);
            if (drawTask.IsCompleted && regionOverlay.Visible)
            {
                drawTask = RenderTick();
            }
        }
    }
    async Task RenderTick()
    {
        await Task.Run(UpdateBuffers);  
        RunShader();  
    }
    public void SetMapMode(string mapModeId)
    {
        selectionManager.SelectRegion(null);
        if (!mapModes.TryGetValue(mapModeId, out mapMode))
        {
            GD.PushError("No Reigstered MapMode found with Id: " + mapModeId);
            mapMode = fallbackMapMode;
        }
        UpdateBuffers();
        mapmodeChanged.Invoke(mapMode);
    }
    public Color GetRegionColor(Region region, out (int,int) borderId, bool isCenter = false)
    {
        borderId = (0,0);
        if (region == null || mapMode == null) return new Color(0, 0, 0, 0);

        Color color = mapMode.GetColor(region, out borderId, isCenter); 
        if (selectionManager.hoveredRegion == region) color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], 0.3f);
        return color;
    }
    public void UpdateRegionColor(Region region)
    {     
        Color color = GetRegionColor(region, out (int,int) borderId, false);
        Color? centralColor = GetRegionColor(region, out (int,int) _, true);;

        foreach (Vector2I tilePos in region.tiles)
        {
            Color finalColor = color;
            if (centralColor != null && region.owner?.capital == region)
            {
                finalColor = (Color)centralColor;
            }

            int index = (tilePos.Y * worldSize.X) + tilePos.X;
            regionColors[index] = finalColor;     
            borderValues[index] = borderId;   
        }
    }
}