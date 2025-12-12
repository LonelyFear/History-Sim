using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject]
public class TradeZone
{
    [Key(0)] public ulong id;

    [Key(1)] public ulong CoTid { get; set; }
    [Key(2)] public HashSet<ulong> regionIds { get; set; } = new HashSet<ulong>(10);
    [Key(3)] public Color color { get; set; }
    [IgnoreMember] static Random rng = new Random();
    
    [IgnoreMember] public static SimManager simManager;
    [IgnoreMember] public static ObjectManager objectManager;
    public void AddRegion(Region region)
    {
        if (region.tradeZone != null && region.tradeZone != this)
        {
            region.tradeZone.RemoveRegion(region);
        }
        if (region.tradeZone != this)
        {
            region.tradeZone = this;
            regionIds.Add(region.id);            
        }
    }
    public void RemoveRegion(Region region)
    {
        if (region.tradeZone == this)
        {
            region.tradeZone = null;
            regionIds.Remove(region.id);
            if (region.id == CoTid)
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