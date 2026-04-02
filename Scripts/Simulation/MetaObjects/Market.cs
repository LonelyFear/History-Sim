using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject]
public class Market : NamedObject
{
    [Key(1)] public ulong centerId { get; set; }
    [Key(2)] public HashSet<ulong> regionIds { get; set; } = [];
    [Key(3)] public Color color { get; set; }
    [Key(4)]  public ulong? controllerId { get; set; } = null;

    public void AddRegion(Region region)
    {
        Market originalMarket = objectManager.GetMarket(region.marketId);
        if (originalMarket != null && region.marketId != id)
        {
            originalMarket.RemoveRegion(region);
        }
        
        if (region.marketId != id)
        {
            region.marketId = id;
            regionIds.Add(region.id);            
        }
    }
    public void RemoveRegion(Region region)
    {
        if (region != null && region.marketId == id)
        {
            region.marketId = null;
            regionIds.Remove(region.id);
            if (region.id == centerId)
            {
                objectManager.DeleteTradeZone(this);
            }
        }
    }
    public int GetZoneSize()
    {
        return regionIds.Count;
    }
}