using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject]
public class TradeZone : NamedObject
{
    [Key(7)] public ulong centerId { get; set; }
    [Key(8)] public HashSet<ulong> regionIds { get; set; } = [];
    [Key(9)] public Color color { get; set; }
    [Key(10)]  public ulong? controllerId { get; set; } = null;

    public void AddRegion(Region region)
    {
        TradeZone originalTradeZone = objectManager.GetTradeZone(region.tradeZoneId);
        if (originalTradeZone != null && region.tradeZoneId != id)
        {
            originalTradeZone.RemoveRegion(region);
        }
        
        if (region.tradeZoneId != id)
        {
            region.tradeZoneId = id;
            regionIds.Add(region.id);            
        }
    }
    public void RemoveRegion(Region region)
    {
        if (region != null && region.tradeZoneId == id)
        {
            region.tradeZoneId = null;
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