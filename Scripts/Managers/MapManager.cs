using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class MapManager : Node2D
{
    Task mapmodeTask;
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


    public override void _Ready()
    {
        simManager = GetNode<SimManager>("/root/Game/Simulation");
        regionOverlay = GetNode<Sprite2D>("RegionOverlay");
    }
    public void InitMapManager(){
        Scale = simManager.world.Scale * simManager.tilesPerRegion;
        worldSize = simManager.worldSize;
        regionImage = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgba8);
        regionOverlay.Texture = ImageTexture.CreateFromImage(regionImage);
        initialized = true;
    }

    public void UpdateRegionColors(){
        foreach (Region region in simManager.regions){
            SetRegionColor(region.pos.X, region.pos.Y, GetRegionColor(region));
        }
    }
    public override void _Process(double delta)
    {
        if (initialized){
            mousePos = GetGlobalMousePosition();
            hoveredRegionPos = simManager.GlobalToRegionPos(mousePos);
            if (hoveredRegionPos.X >= 0 && hoveredRegionPos.X < worldSize.X && hoveredRegionPos.Y >= 0 && hoveredRegionPos.Y < worldSize.Y){
                hoveredRegion = GetRegion(hoveredRegionPos.X, hoveredRegionPos.Y);
                hoveredState = hoveredRegion.owner;
            } else {
                hoveredRegion = null;
                hoveredState = null;
            }
            CheckMapmodeChange();
            Selection();    
        }

    }

    void CheckMapmodeChange(){
        if (mapmodeTask == null || mapmodeTask.IsCompleted){
            if (Input.IsActionJustPressed("MapMode_Polity")){
                SetMapMode(MapModes.POLITIY);
            }         
            else if (Input.IsActionJustPressed("MapMode_Culture")){
                SetMapMode(MapModes.CULTURE);
            }      
            else if (Input.IsActionJustPressed("MapMode_Population")){
                SetMapMode(MapModes.POPULATION);
            }   
        }        
    }

    void Selection(){
        PopObject smo = selectedMetaObj;
        if (selectedMode != mapMode){
            selectedMetaObj = null;
        }
        if (Input.IsActionJustPressed("Select")){
            switch (mapMode){
                case MapModes.POLITIY:
                    if (hoveredRegion.habitable){
                        selectedMetaObj = hoveredRegion;
                        if (hoveredState != null){
                            selectedMetaObj = hoveredState;
                        }
                    } else {
                        selectedMetaObj = null;
                    }
                    break;
                case MapModes.CULTURE:
                    if (hoveredRegion.cultures.Keys.Count > 0){
                        selectedMetaObj = hoveredRegion.cultures.ToArray()[0].Key;
                    } else {
                        selectedMetaObj = null;
                    }
                    break;
            }
        }
        if (selectedMetaObj != smo){
            UpdateAllRegions();
        }
    }

    public void SetMapMode(MapModes mode){
        mapMode = mode;
        UpdateAllRegions();
    }

    void UpdateAllRegions(){
        foreach (Region region in simManager.regions){
            SetRegionColor(region.pos.X, region.pos.Y, GetRegionColor(region));
        }        
    }
    public Color GetRegionColor(Region region){
        Color color = new Color(0, 0, 0, 0);
        switch (mapMode){
            case MapModes.POLITIY:  
                if (region.pops.Count > 0){
                    color = new Color(0.2f, 0.2f, 0.2f);
                }
                if (region.owner != null){
                    color = region.owner.color;
                    if (region.border || region.frontier){
                        color = (color * 0.8f) + (new Color(0, 0, 0) * 0.2f);
                    }
                    if (region.owner.capital == region){
                        //color = new Color(1,0,0);
                    }

                }
                if (selectedMetaObj != null){
                    switch (selectedMetaObj.GetObjectType()){
                        case PopObject.ObjectType.REGION:
                            if (region != selectedMetaObj){
                                color = (color * 0.6f) + (new Color(0, 0, 0) * 0.4f);
                            }
                            break;
                        case PopObject.ObjectType.STATE:
                            if (region.owner != selectedMetaObj){
                                color = (color * 0.6f) + (new Color(0, 0, 0) * 0.4f);
                            }
                            break;
                    }                     
                }
               
            break;
            case MapModes.POPULATION:
                if (region.habitable && region.pops.Count > 0){
                    color = new Color(0, (float)region.population/Pop.ToNativePopulation(1000 * (int)Mathf.Pow(simManager.tilesPerRegion, 2)), 0, 1);
                } else if (region.habitable) {
                    color = new Color(0, 0, 0, 1);
                }
            break;
            case MapModes.CULTURE:
                if (region.habitable && region.pops.Count > 0){
                    color = region.cultures.ElementAt(0).Key.color;
                } else if (region.habitable) {
                    color = new Color(0, 0, 0, 1);
                }
            break;
            case MapModes.POPS:
                if (region.habitable && region.pops.Count > 0){
                    color = new Color(0, 0,(float)region.pops.Count/10, 1);
                } else if (region.habitable) {
                    color = new Color(0, 0, 0, 1);
                }
                
            break;
        }
        // if (hoveredRegion == region){
        //     color = (color * 0.8f) + (new Color(0, 0, 0) * 0.2f);
        // }
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
        mapUpdate = false;
        regionOverlay.Texture = ImageTexture.CreateFromImage(regionImage);
    }
}

public enum MapModes {
    POLITIY,
    POPULATION,
    CULTURE,
    POPS
}