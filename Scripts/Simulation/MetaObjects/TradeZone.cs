using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject]
public class TradeZone
{
    [Key(0)] public ulong id;
    [IgnoreMember]
    public Region CoT { get; set; }
    [Key(1)] public ulong CoTID { get; set; }
    [IgnoreMember]
    public List<Region> regions { get; set; } = new List<Region>();
    [Key(2)] public List<ulong> regionsIDs { get; set; }
    [Key(3)] public Color color { get; set; }
    [IgnoreMember]
    static Random rng = new Random();
    [IgnoreMember] public static SimManager simManager;
    [IgnoreMember] public static ObjectManager objectManager;
    public void PrepareForSave()
    {
        CoTID = CoT != null ? CoT.id : 0;
        regionsIDs = regions.Select(r => r.id).ToList();
    }
    public void LoadFromSave()
    {
        CoT = CoTID == 0 ? null : objectManager.GetRegion(CoTID);
        regions = regionsIDs.Select(r => objectManager.GetRegion(r)).ToList();
    }
    public void AddRegion(Region region)
    {
        if (region.tradeZone != null && region.tradeZone != this)
        {
            region.tradeZone.RemoveRegion(region);
        }
        if (region.tradeZone != this)
        {
            region.tradeZone = this;
            regions.Add(region);            
        }
    }
    public void RemoveRegion(Region region)
    {
        if (region.tradeZone == this)
        {
            region.tradeZone = null;
            regions.Remove(region);
            if (region == CoT)
            {
                objectManager.DeleteTradeZone(this);
            }
        }
    }
    public int GetZoneSize()
    {
        return regions.Count;
    }
}