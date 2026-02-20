using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject]
public class Market : NamedObject
{
    [Key(1)] public ulong centerId { get; set; }
    [Key(2)] public HashSet<ulong> regionIds { get; set; } = new HashSet<ulong>();
    [Key(3)] public Color color { get; set; }
    [Key(4)]  public ulong? controllerId { get; set; } = null;

    public void AddRegion(Region region)
    {
        if (region.marketId != null && region.marketId != id)
        {
            objectManager.GetMarket(region.marketId).RemoveRegion(region);
        }
        if (region.marketId != id)
        {
            region.marketId = id;
            regionIds.Add(region.id);            
        }
    }
    public void RemoveRegion(Region region)
    {
        if (region.marketId == id)
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