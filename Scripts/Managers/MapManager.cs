using Godot;
using System;
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

    public OptionButton mapModeUI;
    public CheckBox showRegionsCheckbox;


    public override void _Ready()
    {
        simManager = GetNode<SimManager>("/root/Game/Simulation");
        regionOverlay = GetNode<Sprite2D>("RegionOverlay");
        mapModeUI = GetNode<OptionButton>("/root/Game/UI/Action Panel/HBoxContainer/MapModeHolder/MapMode");
        showRegionsCheckbox = GetNode<CheckBox>("/root/Game/UI/Action Panel/HBoxContainer/ShowRegionsCheckbox");
    }
    public void InitMapManager(){
        Scale = simManager.world.Scale * simManager.tilesPerRegion;
        worldSize = SimManager.worldSize;
        regionImage = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgba8);
        regionOverlay.Texture = ImageTexture.CreateFromImage(regionImage);
        initialized = true;
    }

    public void UpdateRegionColors(IEnumerable<Region> regions)
    {
        Parallel.ForEach(regions, region =>
        {
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
                    if (hoveredRegion.habitable && hoveredRegion.pops.Count > 0)
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
            }
            if (smo != selectedMetaObj)
            {
                UpdateRegionColors(simManager.regions);
            }
        }
    }

    public void SetMapMode(MapModes mode)
    {
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
                }
                if (region.owner != null)
                {
                    color = region.owner.color;
                    if (region.border || region.frontier)
                    {
                        color = (color * 0.8f) + (new Color(0, 0, 0) * 0.2f);
                    }
                    if (region.owner.capital == region)
                    {
                        //color = new Color(1,0,0);
                    }

                }
                if (region.tradeWeight > 100)
                {
                    color = new Color(1,1,1,1);
                }
                if (selectedMetaObj != null)
                {
                    switch (selectedMetaObj.GetObjectType())
                    {
                        case PopObject.ObjectType.REGION:
                            if (region != selectedMetaObj)
                            {
                                color = (color * 0.6f) + (new Color(0, 0, 0) * 0.4f);
                            }
                            break;
                        case PopObject.ObjectType.STATE:
                            if (region.owner != selectedMetaObj)
                            {
                                color = (color * 0.6f) + (new Color(0, 0, 0) * 0.4f);
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
    public void SetRegionColor(int x, int y, Color color){
        if (regionImage.GetPixel(x,y) != color){
            regionImage.SetPixel(x, y, color);
            mapUpdate = true;            
        }
    }
    public void UpdateMap(){
        if (mapUpdate)
        {
            mapUpdate = false;
            regionOverlay.Texture = ImageTexture.CreateFromImage(regionImage);
        }

    }
}

public enum MapModes {
    POLITIY,
    CULTURE,
    POPULATION,
    TECH,
    POPS
}