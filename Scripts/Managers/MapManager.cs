using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

public partial class MapManager : Node2D
{
    [Export] BorderRenderer borderRenderer;
    Task mapmodeTask = null;
    SimManager simManager;
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
    public OptionButton mapModeUI;
    public CheckBox showRegionsCheckbox;

    int regionResolution = 4;
    public static ObjectManager objectManager;

    // NOTE: Painted regions are updated in TimeManager.cs
    public override void _Ready()
    {
        regionOverlay = GetNode<Sprite2D>("Region Map");
        mapModeUI = GetNode<OptionButton>("/root/Game/UI/Action Panel/HBoxContainer/MapModeHolder/MapMode");
        showRegionsCheckbox = GetNode<CheckBox>("/root/Game/UI/Action Panel/HBoxContainer/ShowRegionsCheckbox");
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += InitMapManager;
	}
    void InitMapManager() {
        simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        simManager.mapManager = this;
        Scale = simManager.terrainMap.Scale * (SimManager.tilesPerRegion/(float)regionResolution);
        worldSize = SimManager.worldSize;
        regionImage = Image.CreateEmpty(worldSize.X * regionResolution, worldSize.Y * regionResolution, true, Image.Format.Rgba8);
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
            hoveredRegionPos = simManager.GlobalToRegionPos(mousePos);

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
                    if (hoveredRegion.pops.Count >= 0 && hoveredRegion.habitable)
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
                case MapModes.POLITIY:
                    if (hoveredRegion.pops.Count >= 0 && hoveredRegion.habitable)
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
    
    public Color GetRegionColor(Region region, bool includeOverlay = true)
    {
        Color color = new Color(0, 0, 0, 0);
        State regionOwner = region.owner;
        MapModes drawnMapMode = mapMode;
        if (!includeOverlay)
        {
            drawnMapMode = MapModes.NONE;
        }
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
                        if (regionOwner.capital == region && regionOwner.vassalManager.sovereignty == Sovereignty.INDEPENDENT)
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
                                color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], 0.4f);
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
                        if (region.owner.capital == region)
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
                                color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], 0.4f);
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
                                color = Utility.MultiColourLerp([cBefore, new Color(0, 0, 0)], 0.4f);;
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
                if (region.largestCultureId != null)
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
                    if (region.cultureIds.ContainsKey(culture.id) && region.largestCultureId != culture.id)
                    {
                        color = culture.color;
                        color = (color * 0.8f) + (new Color(0, 0, 0) * 0.2f);
                    }
                    else if (region.largestCultureId != culture.id)
                    {
                        color = (color * 0.3f) + (new Color(0, 0, 0) * 0.7f);
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
                    if (region.tradeZone != null)
                    {
                        color = region.tradeZone.color;
                    }
                    if (region.isCoT)
                    {
                        color = new Color(1f, 1f, 0f);
                    }
                }
                else if (region.habitable)
                {
                    color = new Color(0, 0, 0, 1);
                }     
                break;
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
            case MapModes.NONE:
                if (selectedMetaObj != null)
                {
                    color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], 0.7f);
                }
                break;
        }
        if (hoveredRegion == region){
            color = Utility.MultiColourLerp([new Color(0, 0, 0), color], 0.7f);
        }
        return color;
    }
    public void UpdateRegionColor(int x, int y)
    {

        Region r = objectManager.GetRegion(x, y);

        Color noneColor = GetRegionColor(r, false);
        Color color = GetRegionColor(r);

        for (int rx = 0; rx < regionResolution; rx++)
        {
            for (int ry = 0; ry < regionResolution; ry++)
            {
                Color finalColor = color;
                int posX = (x * regionResolution) + rx;
                int posY = (y * regionResolution) + ry;

                // Carved Borders
                if (!r.tiles[rx, ry].renderOverlay)
                {
                    finalColor = noneColor;
                }
                if (regionImage.GetPixel(posX, posY) != finalColor * 0.9f)
                {
                    regionImage.SetPixel(posX, posY, finalColor * 0.9f);
                    mapUpdate = true;
                }                
            }
        }
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
    POPS,
    NONE
}