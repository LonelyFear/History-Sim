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
    [Export] TimeManager timeManager;
    [Export] SelectionManager selectionManager;
    Vector2I worldSize;
    [Export] public Sprite2D regionOverlay;
    [Export] public Sprite2D terrainSprite;
    //ImageTexture regionTexture;
    Image regionImage;
    Image terrainImage;
    
    public IMapMode mapMode;
    IMapMode fallbackMapMode = new FallbackMapMode();
    public Dictionary<string, IMapMode> mapModes = [];

    public bool initialized = false;
    [Export] SimManagerHolder simHolder;
    [Export] PlayerCamera playerCamera;

    [Export(PropertyHint.Range, "1,10")] int regionResolution = 4;
     
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

    // Input arrays
    Color[] regionColors;
    ulong[] borderValues;

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

        Scale = simManager.terrainMap.Scale/regionResolution;

        worldSize = SimManager.worldSize;
        borderValues = new ulong[worldSize.X * worldSize.Y];
        regionColors = new Color[worldSize.X * worldSize.Y];

        regionImage = Image.CreateEmpty(worldSize.X*regionResolution, worldSize.Y*regionResolution, false, Image.Format.Rgbaf);
        terrainImage = ((TerrainMap)simManager.terrainMap).terrainMap.Texture.GetImage();

        IMapMode.simManager = simManager;
        IMapMode.selectionManager = selectionManager;
        
        initialized = true;
        InitShader();
    }
    public void InitShader()
    {
        // Creates shader and pipeline
        regionOverlayShader = rd.ShaderCreateFromSpirV(regionShaderFile.GetSpirV());
		regionOverlayShaderPipeline = rd.ComputePipelineCreate(regionOverlayShader);

        // Creates a buffer for world size

        // Creates image buffer
        //GD.Print(worldSize);
        //GD.Print(regionImage.GetSize());
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
    public void UpdateRegionColors(IEnumerable<Region> regions)
    {
        //if (!(bool)regionOverlay.CallDeferred("is_visible")) return;
        var partitioner = Partitioner.Create(regions);
        //borderRenderer.RedrawBorders();
        Parallel.ForEach(partitioner, (region) =>
        {
            UpdateRegionColor(region.centerPos.X, region.centerPos.Y);
        });
        PrepBuffers();
        //RunShader();
    }

    public void PrepBuffers()
    {
        try
        {      
            float[] dimensionsArray = [worldSize.X, worldSize.Y, regionResolution, mapMode.Opacity];
            byte[] dimensionBytes = MemoryMarshal.AsBytes(dimensionsArray.AsSpan()).ToArray();
            dimensionsBuffer= rd.StorageBufferCreate((uint)dimensionBytes.Length, dimensionBytes);  

            byte[] colorsBytes = MemoryMarshal.AsBytes(regionColors.AsSpan()).ToArray();
            colorsBuffer = rd.StorageBufferCreate((uint)colorsBytes.Length, colorsBytes);  

            byte[] borderBytes = MemoryMarshal.AsBytes(borderValues.AsSpan()).ToArray();
            bordersBuffer = rd.StorageBufferCreate((uint)borderBytes.Length, borderBytes); 

            float[] cameraData = [playerCamera.Zoom.X];

            byte[] cameraBytes = MemoryMarshal.AsBytes(cameraData.AsSpan()).ToArray();
            cameraBuffer = rd.StorageBufferCreate((uint)cameraBytes.Length, cameraBytes);             
        } catch (Exception e)
        {
            GD.PushError(e);
        }
 
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
        }
    }
    void UpdateRegionVisibility(bool value) {
        if (value != regionOverlay.Visible)
        {
            regionOverlay.Visible = value;
            if (!value)
            {
                selectionManager.SelectRegion(null);            
            }            
        }
    }
    public void SetMapMode(string mapModeId)
    {
        selectionManager.SelectRegion(null);
        if (!mapModes.TryGetValue(mapModeId, out mapMode))
        {
            GD.PushError("No Reigstered MapMode found with Id: " + mapModeId);
            mapMode = fallbackMapMode;
        }
        UpdateRegionColors(simManager.regionIds.Values);
        mapmodeChanged.Invoke(mapMode);
    }
    public Color GetRegionColor(Region region, out ulong borderId, bool isCenter = false)
    {
        borderId = 0;
        if (region == null || mapMode == null) return new Color(0, 0, 0, 0);

        Color color = mapMode.GetColor(region, out borderId, isCenter); 
        if (selectionManager.hoveredRegion == region) color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], 0.3f);
        return color;
    }
    public void UpdateRegionColor(int x, int y)
    {   
            
        Region r = ObjectManager.GetRegion(simManager.tiles[x,y].regionId);
        if (r == null) return;

        Color color = GetRegionColor(r, out ulong borderId, false);
        Color? centralColor = GetRegionColor(r, out ulong _, true);;

        int month = (int)(TimeManager.GetMonth(timeManager.ticks) - 1);

        foreach (Vector2I tilePos in r.tiles)
        {
            Tile tile = simManager.tiles[tilePos.X, tilePos.Y];
            Color finalColor = color;
            if (centralColor != null && r.owner?.capital == r)
            {
                finalColor = (Color)centralColor;
            }

            int index = (tilePos.Y * worldSize.X) + tilePos.X;
            regionColors[index] = finalColor;     
            borderValues[index] = borderId;   
        }
    }
}