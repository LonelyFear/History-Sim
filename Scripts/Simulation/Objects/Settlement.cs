using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
public class Settlement : NamedObject
{
    public enum Tier
    {
        // The lowest settlement tier
        VILLAGE,
        TOWN,
        CITY,
    }
    [Key(0)] public ulong regionId;
    [Key(1)] public Dictionary<string, BuildingSlot> buildings = new Dictionary<string, BuildingSlot>();
    [Key(2)] public Tier settlementTier = Tier.VILLAGE;
    [IgnoreMember] Region region;
    [Key(3)] public int buildingCount = 0;
    [IgnoreMember] public Dictionary<SocialClass, long> requiredWorkers = new Dictionary<SocialClass, long>();
    [IgnoreMember] public Dictionary<SocialClass, long> maxJobs = new Dictionary<SocialClass, long>();
    public Settlement(){}
    public Settlement(Region r)
    {
        region = r;
        regionId = r.id;
    }
    public void Init()
    {
        region = objectManager.GetRegion(regionId);
    }
    public void UpdateSlots()
    {
        foreach (var pair in buildings.ToArray())
        {
            UpdateBuildingSlot(pair.Key);
        }   
        if (region.owner != null && region.owner.capital != region)
        {
            DestroyBuilding("manor", "downsizing");
        }  
    }
    public void UpdateEmployment()
    {
        // Updates worker requirements
        requiredWorkers = region.professions.ToDictionary(entry => entry.Key, entry => 0L);

        foreach (var pair in buildings)
        {
            Building building = AssetManager.GetBuilding(pair.Key);
            BuildingSlot slot = pair.Value;

            requiredWorkers[building.profession] += slot.maxEmployment;
        }
        // Clones dictionary
        maxJobs = requiredWorkers.ToDictionary(entry => entry.Key, entry => entry.Value);

        // Updates employment
        Dictionary<SocialClass, long> workers = region.professions.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach (var pair in buildings)
        {
            Building building = AssetManager.GetBuilding(pair.Key);
            BuildingSlot slot = pair.Value;
            SocialClass buildingProfession = building.profession;

            slot.currentEmployment = Math.Clamp(workers[buildingProfession], 0, slot.maxEmployment);
            workers[buildingProfession] -= slot.currentEmployment;     
        }
        foreach (var pair in region.professions)
        {
            if (!requiredWorkers.TryGetValue(pair.Key, out long value))
            {
                requiredWorkers[pair.Key] = -region.professions[pair.Key];
            } else
            {
                requiredWorkers[pair.Key] -= region.professions[pair.Key];
            }
        }
    }
    public void PlaceBuilding(string buildingId)
    {
        Building building = AssetManager.GetBuilding(buildingId);
        if (building == null) return;

        if (buildings.ContainsKey(buildingId))
        {
            buildings[buildingId].buildingLevel++;
        } else
        {
            buildings.Add(buildingId, new BuildingSlot()
            {
                buildingLevel = 1
            });            
        }
        UpdateBuildingSlot(buildingId);
    }
    public void DestroyBuilding(string buildingId, string reason)
    {
        if (!buildings.TryGetValue(buildingId, out BuildingSlot value)) return;
        value.buildingLevel--;
        UpdateBuildingSlot(buildingId);
    }
    public void UpdateBuildingSlot(string buildingId)
    {
        BuildingSlot slot = buildings[buildingId];
        slot.maxEmployment = slot.buildingLevel * AssetManager.GetBuilding(buildingId).workersPerLevel;
        if (slot.buildingLevel <= 0)
        {
            buildings.Remove(buildingId);
        }
    }    
}
