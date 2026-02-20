using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

public partial class MapManager : Node2D
{
    [Export] BorderRenderer borderRenderer;
    Task mapmodeTask = null;
    SimManager simManager;
    TimeManager timeManager;
    Vector2I worldSize;
    Sprite2D regionOverlay;
    ImageTexture regionTexture;
    Image regionImage;
    
    public MapModes mapMode;
    public Vector2 mousePos;
    public Vector2I hoveredRegionPos;
    public Region hoveredRegion = null;
    public State hoveredState = null;
    public PopObject selectedMetaObj {get; private set; }
    public MapModes selectedMode;
    public bool mapUpdate = false;
    public bool initialized = false;
    [Export] public OptionButton mapModeUI;
    [Export] public CheckBox showRegionsCheckbox;

    int regionResolution = 4;
    public static ObjectManager objectManager;

    // NOTE: Painted regions are updated in TimeManager.cs
    public override void _Ready()
    {
        regionOverlay = GetNode<Sprite2D>("Region Map");
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += InitMapManager;
	}
    void InitMapManager() {
        simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        timeManager = simManager.timeManager;
        simManager.mapManager = this;
        Scale = simManager.terrainMap.Scale * (SimManager.tilesPerRegion/(float)regionResolution);
        worldSize = SimManager.worldSize;
        regionImage = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgba8);
        regionTexture = ImageTexture.CreateFromImage(regionImage);
        regionOverlay.Texture = regionTexture;
        initialized = true;
    }
    public void UpdateRegionColors(IEnumerable<Region> regions)
    {
        if (!regionOverlay.Visible) return;
        var partitioner = Partitioner.Create(regions);
        //borderRenderer.RedrawBorders();
        Parallel.ForEach(partitioner, (region) =>
        {
            if (region != null)
            {
                UpdateRegionColor(region.pos.X, region.pos.Y);
            }
        });
    }
    public override void _Process(double delta)
    {
        if (initialized)
        {
            UpdateRegionVisibility(showRegionsCheckbox.ButtonPressed);
            
            mousePos = GetGlobalMousePosition();
            hoveredRegionPos = simManager.GlobalToTilePos(mousePos);

            UpdateHovering();

            if (regionOverlay.Visible)
            {
                CheckMapmodeChange();
                UpdateMap();
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
            borderRenderer.Visible = value;
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
                            newSelected = hoveredState.vassalManager.GetOverlord(true);
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
    
    public Color GetRegionColor(Region region, bool includeOverlay = true, bool includeCapital = false)
    {
        float colorDarkness = 0.4f;
        Color color = new Color(0, 0, 0, 0);
        if (region == null) return color;
        State regionOwner = region.owner;
        MapModes drawnMapMode = mapMode;
        int month = (int)(timeManager.GetMonth() - 1);
        switch (drawnMapMode)
        {
            case MapModes.REALM:
                if (region.pops.Count > 0)
                {
                    color = new Color(0.2f, 0.2f, 0.2f);
                    if (regionOwner != null)
                    {
                        color = regionOwner.displayColor;
                        if (region.occupier != null)
                        {
                            color = region.occupier.displayColor;
                        }
                        if (regionOwner.capital == region && regionOwner.vassalManager.sovereignty == Sovereignty.INDEPENDENT && includeCapital)
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
                            if (region.owner == null || region.owner.vassalManager.GetOverlord(true) != ((State)selectedMetaObj).vassalManager.GetOverlord(true))
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
                    color = new Color(0.2f, 0.2f, 0.2f);
                    if (region.owner != null)
                    {
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
                            if (region.owner.vassalManager.GetOverlord(true) == selectedMetaObj)
                            {
                                color = Utility.MultiColourLerp([cBefore, new Color(0, 0, 0)], colorDarkness);
                            }
                            break;
                    }
                }

                break;
            case MapModes.POPULATION:
                if (region.habitable && region.pops.Count > 0)
                {
                    long regionPopulation = Pop.ToNativePopulation(1000 * (int)Mathf.Pow(SimManager.tilesPerRegion, 2));
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
                    float indAverage = 0;
                    float milAverage = 0;
                    float sciAverage = 0;
                    float socAverage = 0;
                    foreach (Pop pop in region.pops.ToArray())
                    {
                        socAverage += pop.tech.societyLevel;
                        milAverage += pop.tech.militaryLevel;
                        sciAverage += pop.tech.scienceLevel;
                        indAverage += pop.tech.industryLevel;
                    }
                    indAverage /= region.pops.Count;
                    milAverage /= region.pops.Count;
                    sciAverage /= region.pops.Count;
                    socAverage /= region.pops.Count;

                    color = new Color(milAverage / 20f, indAverage / 20f, socAverage / 20f);
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
                    color = Utility.MultiColourLerp([new Color(0f, 0f, 0f), new Color(1f, 1f, 1f)], region.GetTradeWeight() / simManager.maxTradeWeight);
                    if (region.marketId != null)
                    {
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
            /*
            case MapModes.POPS:
                if (region.habitable && region.pops.Count > 0)
                {
                    color = new Color(0, 0, (float)region.pops.Count / 10, 1);
                }
                else if (region.habitable)
                {
                    color = new Color(0, 0, 0, 1);
                }
                break;
            */
            case MapModes.TERRAIN_TYPE:
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

        Color noneColor = GetRegionColor(r, false);
        Color color = GetRegionColor(r);
        Color centralColor = GetRegionColor(r, true, true);
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
                default:
                    // Carved Borders
                    if (tile.renderOverlay && !IsMapModeCarved())
                    {
                        finalColor = noneColor;
                    }
                    if (r.isWater && r.pops.Count > 0)
                    {
                        //finalColor = new Color(1, 0.5f, 0);
                    }

                break;                   
            }
            if (regionImage.GetPixel(tile.pos.X, tile.pos.Y) != finalColor * 0.9f)
            {
                regionImage.SetPixel(tile.pos.X, tile.pos.Y, finalColor * 0.9f);
                mapUpdate = true;
            }                
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
    void UpdateMap(){
        if (mapUpdate)
        {
            mapUpdate = false;
            regionTexture.Update(regionImage);
            regionOverlay.Texture = regionTexture;
        }
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