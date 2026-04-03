using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


public partial class MapManager : Node2D
{
    Task mapmodeTask = null;
    SimManager simManager;
    TimeManager timeManager;
    Vector2I worldSize;
    public Sprite2D regionOverlay;
    //ImageTexture regionTexture;
    Image regionImage;
    Image terrainImage;
    
    public MapModes mapMode;
    public Vector2 mousePos;
    public Vector2I hoveredRegionPos;
    public Region hoveredRegion = null;
    public State hoveredState = null;
    public PopObject selectedMetaObj {get; private set; }
    public MapModes selectedMode;
    public bool initialized = false;
    [Export] PlayerCamera playerCamera;
    [Export] OptionButton mapModeUI;
    [Export] Button deselectMetaObjectButton;
    [Export] public CheckBox showRegionsCheckbox;

    [Export(PropertyHint.Range, "1,10")] int regionResolution = 4;
    public static ObjectManager objectManager;
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
    ColorData colorData = new ColorData();
    Color[] regionColors;
    ulong[] borderValues;
    //float[] colorValues;

    // NOTE: Painted regions are updated in TimeManager.cs
    public override void _Ready()
    {
        regionOverlay = GetNode<Sprite2D>("Region Map");
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += InitMapManager;
        deselectMetaObjectButton.Pressed += () => SelectMetaObject(null);
	}
    void InitMapManager() {
        // Frees dimension buffer
        if (colorsBuffer.IsValid) rd.FreeRid(colorsBuffer);
        if (dimensionsBuffer.IsValid) rd.FreeRid(dimensionsBuffer);
        if (terrainTexture.IsValid) rd.FreeRid(terrainTexture);

        simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        timeManager = simManager.timeManager;
        simManager.mapManager = this;

        Scale = simManager.terrainMap.Scale/regionResolution;

        worldSize = SimManager.worldSize;
        borderValues = new ulong[worldSize.X * worldSize.Y];
        regionColors = new Color[worldSize.X * worldSize.Y];

        regionImage = Image.CreateEmpty(worldSize.X*regionResolution, worldSize.Y*regionResolution, false, Image.Format.Rgbaf);
        terrainImage = ((TerrainMap)simManager.terrainMap).terrainMap.Texture.GetImage();
        GD.Print(terrainImage.GetFormat());

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
            UpdateRegionColor(region.pos.X, region.pos.Y);
        });
        PrepBuffers();
        //RunShader();
    }

    public void PrepBuffers()
    {
        try
        {      
            float[] dimensionsArray = [worldSize.X, worldSize.Y, regionResolution, GetMapmodeOpacity()];
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

		rd.FreeRid(uniformSet);
        rd.FreeRid(colorsBuffer);
        rd.FreeRid(bordersBuffer);
        rd.FreeRid(cameraBuffer);
    }
    public override void _Process(double delta)
    {
        if (initialized)
        {
            //UpdateRegionVisibility(showRegionsCheckbox.ButtonPressed);
            
            mousePos = GetGlobalMousePosition();
            hoveredRegionPos = simManager.GlobalToTilePos(mousePos);

            UpdateHovering();

            if (regionOverlay.Visible)
            {
                CheckMapmodeChange();
            }
        }

    }
    void UpdateHovering()
    {
        Region lastHovered = hoveredRegion;
        if (hoveredRegionPos.X >= 0 && hoveredRegionPos.X < worldSize.X && hoveredRegionPos.Y >= 0 && hoveredRegionPos.Y < worldSize.Y && regionOverlay.Visible)
        {
            hoveredRegion = simManager.objectManager.GetRegion(hoveredRegionPos.X, hoveredRegionPos.Y);
            UpdateRegionColor(hoveredRegion.pos.X, hoveredRegion.pos.Y);
            hoveredState = hoveredRegion.owner;
        }
        else
        {
            hoveredRegion = null;
            hoveredState = null;
        }

        if (lastHovered != null)
        {
            UpdateRegionColor(lastHovered.pos.X, lastHovered.pos.Y);
        }
    }

    void UpdateRegionVisibility(bool value) {
        if (value != regionOverlay.Visible)
        {
            regionOverlay.Visible = value;
            if (!value)
            {
                hoveredRegion = null;
                hoveredState = null;              
            }            
        }

    }

    void CheckMapmodeChange(){
        MapModes lastMode = mapMode;
        mapMode = (MapModes)mapModeUI.Selected;
        if (mapmodeTask == null || mapmodeTask.IsCompleted )
        {
            if (Input.IsActionJustPressed("MapMode_Polity"))
            {
                SetMapMode(MapModes.POLITIY);
            }
            else if (Input.IsActionJustPressed("MapMode_Culture"))
            {
                SetMapMode(MapModes.CULTURE);
            }
            else if (Input.IsActionJustPressed("MapMode_Population"))
            {
                SetMapMode(MapModes.POPULATION);
            }
            else if (lastMode != mapMode)
            {
                SetMapMode(mapMode);
            }
        }        
    }
    public void SelectMetaObject(PopObject newObject)
    {
        if (newObject != selectedMetaObj)
        {
            selectedMetaObj = newObject;
            UpdateRegionColors(simManager.regionIds.Values);
        }   
    }
    public override void _UnhandledInput(InputEvent evnt)
    {
        if (evnt.IsAction("Select") && hoveredRegion != null)
        {
            PopObject newSelected = selectedMetaObj;
            switch (mapMode)
            {
                case MapModes.REALM:
                    if (hoveredRegion.habitable)
                    {
                        newSelected = hoveredRegion;
                        if (hoveredState != null)
                        {
                            newSelected = hoveredState.diplomacy.GetOverlord();
                        }
                    }
                    else
                    {
                        newSelected = null;
                    }
                    break;
                case MapModes.RAINFALL:
                    newSelected = hoveredRegion;
                    break;
                case MapModes.POLITIY:
                    if (hoveredRegion.habitable)
                    {
                        newSelected = hoveredRegion;
                        if (hoveredState != null)
                        {
                            newSelected = hoveredState;
                        }
                    }
                    else
                    {
                        newSelected = null;
                    }
                    break;
                case MapModes.CULTURE:
                    if (hoveredRegion.cultureIds.Keys.Count > 0)
                    {
                        newSelected = objectManager.GetCulture(hoveredRegion.largestCultureId);
                    }
                    else
                    {
                        newSelected = null;
                    }
                    break;
                case MapModes.TRADE_WEIGHT:
                    if (hoveredRegion.pops.Count >= 0 && hoveredRegion.habitable)
                    {
                        newSelected = hoveredRegion;
                    }
                    break;
            }
            SelectMetaObject(newSelected);
        }
    }

    public void SetMapMode(MapModes mode)
    {
        SelectMetaObject(null);
        mapMode = mode;
        mapModeUI.Selected = (int)mode;
        UpdateRegionColors(simManager.regionIds.Values);
    }
    float GetMapmodeOpacity()
    {
        return mapMode switch
        {
            MapModes.REALM => 0.8f,
            MapModes.POLITIY => 0.8f,
            MapModes.CULTURE => 0.8f,
            MapModes.TRADE_WEIGHT => 0.8f,
            _ => 1f,
        };
    }
    public Color GetRegionColor(Region region, out ulong borderId, bool includeCapital = false)
    {
        float colorDarkness = 0.4f;
        Color color = new Color(0, 0, 0, 0);
        borderId = 0;

        if (region == null) return color;
        State regionOwner = region.owner;
        MapModes drawnMapMode = mapMode;
        int month = (int)(timeManager.GetMonth() - 1);

        switch (drawnMapMode)
        {
            case MapModes.REALM:
                if (region.pops.Count > 0)
                {
                    borderId = 1;
                    color = new Color(0.2f, 0.2f, 0.2f, 1);
                    if (regionOwner != null)
                    {
                        borderId = regionOwner.diplomacy.GetOverlord().id;
                        color = regionOwner.displayColor;
                        if (region.occupier != null)
                        {
                            color = region.occupier.displayColor;
                        }
                        if (regionOwner.capital == region && regionOwner.sovereignty == Sovereignty.INDEPENDENT && includeCapital)
                        {
                            color = region.owner.capitalColor;
                        }
                    }                 
                }
               
                if (selectedMetaObj != null)
                {
                    switch (selectedMetaObj.GetObjectType())
                    {
                        case ObjectType.REGION:
                            if (region != selectedMetaObj)
                            {
                                color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], colorDarkness);
                            }
                            break;
                        case ObjectType.STATE:
                            Color cBefore = color;
                            // Darkens Unrelated Regions
                            if (region.owner == null || region.owner.diplomacy.GetOverlord() != ((State)selectedMetaObj).diplomacy.GetOverlord())
                            {
                                color = Utility.MultiColourLerp([cBefore, new Color(0, 0, 0)], 0.7f);
                            }                           
                            break;
                    }
                }
                break;
            case MapModes.POLITIY:
                if (region.pops.Count > 0)
                {
                    borderId = 1;
                    color = new Color(0.2f, 0.2f, 0.2f, 1);
                    if (region.owner != null)
                    {
                        borderId = region.owner.id;

                        color = region.owner.displayColor;
                        if (region.occupier != null)
                        {
                            color = region.occupier.displayColor;
                        }
                        if (region.owner.capital == region && includeCapital)
                        {
                            color = region.owner.capitalColor;
                        }
                    }                 
                }
               
                if (selectedMetaObj != null)
                {
                    switch (selectedMetaObj.GetObjectType())
                    {
                        case ObjectType.REGION:
                            if (region != selectedMetaObj)
                            {
                                color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], colorDarkness);
                            }
                            break;
                        case ObjectType.STATE:
                            Color cBefore = color;
                            // Darkens unrelated states
                            if (region.owner != selectedMetaObj)
                            {
                                color = Utility.MultiColourLerp([cBefore, new Color(0, 0, 0)], 0.7f);
                            }
                            if (region.owner == null || region.owner == selectedMetaObj)
                            {
                                break;
                            }

                            // Highlights Realms
                            if (region.owner.diplomacy.GetOverlord() == selectedMetaObj)
                            {
                                color = Utility.MultiColourLerp([cBefore, new Color(0, 0, 0)], colorDarkness);
                            }
                            break;
                    }
                }

                break;
            case MapModes.POPULATION:
                colorData.opacity = 1f;
                if (region.habitable && region.pops.Count > 0)
                {
                    long regionPopulation = 1000 * (int)Mathf.Pow(SimManager.tilesPerRegion, 2);
                    color = new Color(0, region.population / Mathf.Max(simManager.highestPopulation, regionPopulation), 0, 1);
                }
                else if (region.habitable)
                {
                    color = new Color(0, 0, 0, 1);
                }
                break;
            case MapModes.CULTURE:

                if (region.largestCultureId != null && region.habitable)
                {
                    borderId = (ulong)region.largestCultureId;
                    color = objectManager.GetCulture(region.largestCultureId).color;
                }
                else if (region.habitable)
                {
                    color = new Color(0, 0, 0, 1);
                }

                if (selectedMetaObj != null && selectedMetaObj.GetObjectType() == ObjectType.CULTURE)
                {
                    Culture culture = (Culture)selectedMetaObj;
                    if (region.cultureIds.ContainsKey(culture.id) && region.largestCultureId != culture.id && region.habitable)
                    {
                        color = culture.color;
                        color = (color * 0.8f) + (new Color(0, 0, 0) * 0.2f);
                    }
                    else if (region.largestCultureId != culture.id || !region.habitable)
                    {
                        color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], colorDarkness);
                    }
                }                
                break;
            case MapModes.TECH:
                if (region.habitable)
                {
                    region.GetAverageTech();
                    color = new Color(region.averageTech.militaryLevel / 20f, region.averageTech.industryLevel / 20f, region.averageTech.societyLevel / 20f);
                }
                break;
            case MapModes.WEALTH:
                if (region.habitable && region.pops.Count > 0)
                {
                    color = Utility.MultiColourLerp([new Color(0f, 0f, 0f), new Color(1f, 1f, 0f)], region.wealth / simManager.maxWealth);
                }
                else if (region.habitable)
                {
                    color = new Color(0, 0, 0, 1);
                }
                break;
            case MapModes.TRADE_WEIGHT:
                if (region.habitable && region.pops.Count > 0)
                {
                    color = Utility.MultiColourLerp([new Color(0f, 0f, 0f), new Color(1f, 1f, 1f)], region.tradeWeight / simManager.maxTradeWeight);
                    if (region.marketId != null)
                    {
                        borderId = (ulong)region.marketId;
                        color = objectManager.GetMarket(region.marketId).color;
                    }
                    if (region.isMarketCenter && includeCapital)
                    {
                        color = new Color(1f, 1f, 0f);
                    }
                }
                else if (region.habitable)
                {
                    color = new Color(0, 0, 0, 1);
                }     
                break;
            case MapModes.TERRAIN_TYPE:
                borderId = (ulong)region.terrainType;
                switch (region.terrainType)
                {
                    case TerrainType.SHALLOW_WATER:
                        color = new Color(0.5f, 0.5f, 1f);
                        break;
                    case TerrainType.DEEP_WATER:
                        color = new Color(0, 0, 0.8f);
                        break;
                    case TerrainType.LAND:
                        color = new Color(0f, 1f, 0f);
                        break;
                    case TerrainType.HILLS:
                        color = new Color(0.4f, 0.4f, 0f);
                        break;
                    case TerrainType.MOUNTAINS:
                        color = new Color(0.1f, 0.1f, 0.1f);
                        break;
                    case TerrainType.ICE:
                        color = new Color(1f, 1f, 1f);
                        break;
                }
                break;
            case MapModes.NONE:
                if (selectedMetaObj != null)
                {
                    color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], colorDarkness);
                }
                break;
            case MapModes.DAY_LENGTH:
                float opacity = 0.75f;
                Tile tile = simManager.tiles[region.pos.X, region.pos.Y];
                color = Utility.MultiColourLerp([new Color(0,0,1, opacity), new Color(1,0,0, opacity)], 
                Mathf.InverseLerp(0, 24, tile.GetDaylightForMonth(month)));
                break;
        }
        if (hoveredRegion == region){
            color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], 0.3f);
        }
        
        return color;
    }
    public void UpdateRegionColor(int x, int y)
    {   
            
        Region r = objectManager.GetRegion(simManager.tiles[x,y].regionId);
        if (r == null) return;

        ulong borderId = 0;
        Color color = GetRegionColor(r, out borderId);

        Color centralColor = GetRegionColor(r, out ulong _, true);
        int month = (int)(timeManager.GetMonth() - 1);

        foreach (Vector2I tilePos in r.tiles)
        {
            Tile tile = simManager.tiles[tilePos.X, tilePos.Y];
            Color finalColor = color;
            if (tilePos == r.pos)
            {
                finalColor = centralColor;
            }
            
            float opacity = 0.75f;
            
            switch (mapMode)
            {
                case MapModes.RAINFALL:
                    finalColor = Utility.MultiColourLerp([Color.Color8(69, 4, 87, (byte)(opacity * 255f)), Color.Color8(31, 160, 136, (byte)(opacity * 255f)), Color.Color8(253, 231, 37, (byte)(opacity * 255f))], 
                    Mathf.InverseLerp(0, 160, tile.GetRainfallForMonth(month)));
                break;
                case MapModes.TEMPERATURE:
                    finalColor = Utility.MultiColourLerp([new Color(0,0,1, opacity), new Color(1,1,1, opacity), new Color(1,0, 0, opacity)], 
                    Mathf.InverseLerp(-40, 40, tile.GetTempForMonth(month)));
                    break;
                case MapModes.CONTINENTIALITY:
                    finalColor = Utility.MultiColourLerp([new Color(0,0,1, opacity), new Color(1,0,0, opacity)], 
                    Mathf.InverseLerp(0, 1, tile.continentiality));
                    break;                
            }
            /*
            Vector2I pos = tilePos * regionResolution;
            for (int dx = 0; dx < regionResolution; dx++)
            {
                for (int dy = 0; dy < regionResolution; dy++)
                {
                    int index = ((pos.Y + dy) * worldSize.X * regionResolution) + (pos.X + dx);
                    regionColors[index] = finalColor;                     
                }                
            }
            */
            int index = (tilePos.Y * worldSize.X) + tilePos.X;
            regionColors[index] = finalColor;     
            borderValues[index] = borderId;   
        }
    }
    public bool IsMapModeCarved()
    {
        if (mapMode == MapModes.TERRAIN_TYPE)
        {
            return false;
        }
        return true;
    }

    struct ColorData()
    {
        public Color[] colors;
        public float opacity;
    }
}

public enum MapModes {
    REALM,
    POLITIY,
    CULTURE,
    POPULATION,
    TECH,
    WEALTH,
    TRADE_WEIGHT,
    TERRAIN_TYPE,
    TEMPERATURE,
    RAINFALL,
    DAY_LENGTH,
    CONTINENTIALITY,
    NONE
}