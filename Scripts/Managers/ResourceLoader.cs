using Godot;
using System.Collections.Generic;
using System.Text.Json;
public static class ResourceLoader
{
    // Saved Stuff
    public static Dictionary<string, SimResource> resources = new Dictionary<string, SimResource>();
    public static Dictionary<string, BuildingData> buildings = new Dictionary<string, BuildingData>();
    public static void LoadBuildings()
    {
        string buildingsPath = @"Data/Buildings/";
        DirAccess buildingDir = DirAccess.Open(buildingsPath);
        if (buildingDir != null)
        {
            foreach (string buildingFile in buildingDir.GetFiles())
            {
                string path = buildingsPath + buildingFile;

                string buildingData = FileAccess.Open(path, FileAccess.ModeFlags.Read).GetAsText();

                BuildingData building = JsonSerializer.Deserialize<BuildingData>(buildingData);

                buildings.Add(building.id, building);
                foreach (string id in building.resourcesProducedIds.Keys)
                {
                    if (GetResource(id) == null)
                    {
                        GD.PushError("Building couldnt load resource '" + id + "'");
                        return;
                    }
                    building.resourcesProduced.Add(GetResource(id), building.resourcesProducedIds[id]);
                }
            }

        }
        else
        {
            GD.PushError("Buildings directory not found at path '" + buildingsPath + "'");
        }
    }
    public static void LoadResources()
    {
        string resourcesPath = @"Data/Resources/";

        DirAccess resourcesDir = DirAccess.Open(resourcesPath);
        if (resourcesDir != null)
        {
            foreach (string resourcesFile in resourcesDir.GetFiles())
            {
                string path = resourcesPath + resourcesFile;

                string resourceData = FileAccess.Open(path, FileAccess.ModeFlags.Read).GetAsText();
                SimResource resource = JsonSerializer.Deserialize<SimResource>(resourceData);

                resources.Add(resource.id, resource);
            }

        }
        else
        {
            GD.PushError("Resources directory not found at path '" + resourcesPath + "'");
        }
    }
    public static SimResource GetResource(string id)
    {
        if (resources.ContainsKey(id))
        {
            return resources[id];
        }
        else
        {
            GD.PushError("Resource not found with ID '" + id + "'");
            return null;
        }
    }
    public static BuildingData GetBuilding(string id)
    {
        if (buildings.ContainsKey(id))
        {
            return buildings[id];
        }
        else
        {
            GD.PushError("Building not found with ID '" + id + "'");
            return null;
        }
    }
}