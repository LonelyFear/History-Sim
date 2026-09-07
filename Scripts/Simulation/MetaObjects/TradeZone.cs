using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject]
public class TradeZone : NamedObject
{
    [Key(7)] public ulong centerId { get; set; }
    [Key(8)] public ulong[] regionIds { get; set; }
    [IgnoreMember] public HashSet<Region> regions { get; set; } = [];
    [Key(9)] public Color color { get; set; }
    [Key(10)]  public ulong? controllerId { get; set; } = null;
    [Key(11)] public float totalMarketWeight = 1f;

    public override void PrepareForSave()
    {
        base.PrepareForSave();
        //economy.PrepareForSave();
    }
    public override void LoadFromSave()
    {
        base.LoadFromSave();
        //economy.LoadFromSave();
    }

    public void AddRegion(Region region)
    {
        if (region.tradeZone == this) return;

        TradeZone originalTradeZone = region.tradeZone;

        bool isTradeZoneCapital = originalTradeZone != null && originalTradeZone.centerId == region.id;
        Region[] originalRegions = originalTradeZone != null ? [..originalTradeZone.regions] : null;

        originalTradeZone?.RemoveRegion(region);
        region.tradeZone = this;
        regions.Add(region);            

        if (isTradeZoneCapital)
        {
            foreach (Region r in originalRegions)
            {
                AddRegion(r);
            }
        }
    }
    public void RemoveRegion(Region region)
    {
        if (region != null && region.tradeZone == this)
        {
            region.tradeZone = null;
            
            regions.Remove(region);
            if (region.id == centerId)
            {
                Die();
            }
        }
    }
    public override void Die()
    {
        ObjectManager.DeleteTradeZone(this);
    }

    public int GetZoneSize()
    {
        return regions.Count;
    }
}