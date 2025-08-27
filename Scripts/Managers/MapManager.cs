using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

public partial class MapManager : Area2D
{
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
    public PopObject selectedMetaObj;
    public MapModes selectedMode;
    public bool mapUpdate = false;
    public bool initialized = false;
    int regionResolution = 1;

    public OptionButton mapModeUI;
    public CheckBox showRegionsCheckbox;

    // NOTE: Painted regions are updated in TimeManager.cs
    public override void _Ready()
    {
        regionOverlay = GetNode<Sprite2D>("Region Map");
        mapModeUI = GetNode<OptionButton>("/root/Game/UI/Action Panel/HBoxContainer/MapModeHolder/MapMode");
        showRegionsCheckbox = GetNode<CheckBox>("/root/Game/UI/Action Panel/HBoxContainer/ShowRegionsCheckbox");
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += InitMapManager;
	}
    public void InitMapManager() {
        simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        Scale = simManager.terrainMap.Scale * (simManager.tilesPerRegion/(float)regionResolution);
        worldSize = SimManager.worldSize;
        //GD.Print(worldSize.X * regionResolution);
        regionImage = Image.CreateEmpty(worldSize.X * regionResolution, worldSize.Y * regionResolution, true, Image.Format.Rgba8);
        regionTexture = ImageTexture.CreateFromImage(regionImage);
        regionOverlay.Texture = regionTexture;
        initialized = true;
    }

    public void UpdateRegionColors(IEnumerable<Region> regions)
    {
        // TODO: Fix null region bug
        var partitioner = Partitioner.Create(regions);
        Parallel.ForEach(partitioner, (region) =>
        {
            if (region == null)
            {
                GD.Print(SimManager.worldSize.X * SimManager.worldSize.Y);
                GD.Print(simManager.paintedRegions.Count);
            }
            SetRegionColor(region.pos.X, region.pos.Y, GetRegionColor(region));
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
            hoveredRegion = GetRegion(hoveredRegionPos.X, hoveredRegionPos.Y);
            SetRegionColor(hoveredRegion.pos.X, hoveredRegion.pos.Y, GetRegionColor(hoveredRegion));
            hoveredState = hoveredRegion.owner;
        }
        else
        {
            hoveredRegion = null;
            hoveredState = null;
        }

        if (lastHovered != null)
        {
            SetRegionColor(lastHovered.pos.X, lastHovered.pos.Y, GetRegionColor(lastHovered));
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

    void ShowRegions()
    {
        regionOverlay.Visible = true;
        UpdateRegionColors(simManager.regions);
    }

    void HideRegions()
    {
        regionOverlay.Visible = false;
        hoveredRegion = null;
        hoveredState = null;       
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

    public override void _UnhandledInput(InputEvent evnt)
    {
        PopObject smo = selectedMetaObj;
        if (evnt.IsAction("Select") && hoveredRegion != null)
        {
            switch (mapMode)
            {
                case MapModes.POLITIY:
                    if (hoveredRegion.pops.Count >= 0 && hoveredRegion.habitable)
                    {
                        selectedMetaObj = hoveredRegion;
                        if (hoveredState != null)
                        {
                            selectedMetaObj = hoveredState;
                        }
                    }
                    else
                    {
                        selectedMetaObj = null;
                    }
                    break;
                case MapModes.CULTURE:
                    if (hoveredRegion.cultures.Keys.Count > 0)
                    {
                        selectedMetaObj = hoveredRegion.largestCulture;
                    }
                    else
                    {
                        selectedMetaObj = null;
                    }
                    break;
                case MapModes.TRADE_WEIGHT:
                    if (hoveredRegion.pops.Count >= 0 && hoveredRegion.habitable)
                    {
                        selectedMetaObj = hoveredRegion;
                    }
                    break;
            }
            if (smo != selectedMetaObj)
            {
                UpdateRegionColors(simManager.regions);
            }
        }
    }

    public void SetMapMode(MapModes mode)
    {
        selectedMetaObj = null;
        UpdateRegionColors(simManager.regions);
        mapMode = mode;
        mapModeUI.Selected = (int)mode;
        UpdateRegionColors(simManager.habitableRegions);
    }
    
    public Color GetRegionColor(Region region)
    {
        Color color = new Color(0, 0, 0, 0);
        switch (mapMode)
        {
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
                    /*
                    if (simManager.tradeCenters.Contains(region))
                    {
                        color = Utility.MultiColourLerp([color, new Color(0f, 0f, 0f)], 0.9f);
                    } 
                    */                    
                }
               
                if (selectedMetaObj != null)
                {
                    switch (selectedMetaObj.GetObjectType())
                    {
                        case PopObject.ObjectType.REGION:
                            if (region != selectedMetaObj)
                            {
                                color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], 0.4f);
                            }
                            break;
                        case PopObject.ObjectType.STATE:
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
                            if (region.owner.GetHighestLiege() == selectedMetaObj)
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
                    color = new Color(0, (float)region.population / Pop.ToNativePopulation(1000 * (int)Mathf.Pow(simManager.tilesPerRegion, 2)), 0, 1);
                }
                else if (region.habitable)
                {
                    color = new Color(0, 0, 0, 1);
                }
                break;
            case MapModes.CULTURE:
                if (region.largestCulture != null)
                {
                    color = region.largestCulture.color;
                }
                else if (region.habitable)
                {
                    color = new Color(0, 0, 0, 1);
                }

                if (selectedMetaObj != null && selectedMetaObj.GetObjectType() == PopObject.ObjectType.CULTURE)
                {
                    Culture culture = (Culture)selectedMetaObj;
                    if (region.cultures.ContainsKey(culture) && region.largestCulture != culture)
                    {
                        color = culture.color;
                        color = (color * 0.8f) + (new Color(0, 0, 0) * 0.2f);
                    }
                    else if (region.largestCulture != culture)
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
        }
        if (hoveredRegion == region){
            color = (color * 0.8f) + (new Color(0, 0, 0) * 0.2f);
        }
        return color;
    }
    public Region GetRegion(int x, int y){
        int lx = Mathf.PosMod(x, worldSize.X);
        int ly = Mathf.PosMod(y, worldSize.Y);

        int index = (lx * worldSize.Y) + ly;
        return simManager.regions[index];
    }
    public void SetRegionColor(int x, int y, Color color)
    {
        /*
        if (regionImage.GetPixel(x, y) != color)
        {
            regionImage.SetPixel(x, y, color);
            mapUpdate = true;
        }
        */
        Region r = GetRegion(x, y);
        for (int rx = 0; rx < regionResolution; rx++)
        {
            for (int ry = 0; ry < regionResolution; ry++)
            {
                int posX = (x * regionResolution) + rx;
                int posY = (y * regionResolution) + ry;
                Color finalColor = color;
                int resMinus = regionResolution - 1;
                // Lines

                // Top
                if (mapMode == MapModes.POLITIY && regionResolution > 3)
                {
                    Color borderColor = color;
                    if (ry == 0 && r.DrawBorder(r.borderingRegions[1], ref borderColor))
                    {
                        finalColor = borderColor;
                    }
                    // Left
                    if (rx == 0 && r.DrawBorder(r.borderingRegions[0], ref borderColor))
                    {
                        finalColor = borderColor;
                    }
                    // Bottom
                    if (ry == resMinus && r.DrawBorder(r.borderingRegions[2], ref borderColor))
                    {
                        finalColor = borderColor;
                    }
                    // Right
                    if (rx == resMinus && r.DrawBorder(r.borderingRegions[3], ref borderColor))
                    {
                        finalColor = borderColor;
                    }                    
                }

                /*
                // Corners

                // Top Left
                if (rx == 0 && ry == 0 && r.DrawBorder(r.diagonalRegions[0]))
                {
                    finalColor = new Color(0, 0, 0);
                }
                // Bottom Left
                if (rx == 0 && ry == resMinus && r.DrawBorder(r.diagonalRegions[1]))
                {
                    finalColor = new Color(0, 0, 0);
                }
                // Top Right
                if (rx == resMinus && ry == 0 && r.DrawBorder(r.diagonalRegions[2]))
                {
                    finalColor = new Color(0, 0, 0);
                }
                // Bottom Right
                if (rx == resMinus && ry == resMinus && r.DrawBorder(r.diagonalRegions[3]))
                {
                    finalColor = new Color(0, 0, 0);
                }
                */

                if (regionImage.GetPixel(posX, posY) != finalColor)
                {
                    regionImage.SetPixel(posX, posY, finalColor);
                    mapUpdate = true;
                }
            }
        }
    }
    public void UpdateMap(){
        if (mapUpdate)
        {
            mapUpdate = false;
            regionTexture.Update(regionImage);
            regionOverlay.Texture = regionTexture;
        }
    }
}

public enum MapModes {
    POLITIY,
    CULTURE,
    POPULATION,
    TECH,
    WEALTH,
    TRADE_WEIGHT,
    POPS
}