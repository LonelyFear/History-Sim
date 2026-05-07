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
    [Key(11)] public Economy economy = new();

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
        if (region.tradeZoneId == id) return;

        TradeZone originalTradeZone = objectManager.GetTradeZone(region.tradeZoneId);

        bool isTradeZoneCapital = originalTradeZone != null && originalTradeZone.centerId == region.id;
        Region[] originalRegions = originalTradeZone != null ? [..originalTradeZone.regions] : null;

        originalTradeZone?.RemoveRegion(region);
        region.tradeZoneId = id;
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
        if (region != null && region.tradeZoneId == id)
        {
            region.tradeZoneId = null;
            
            regions.Remove(region);
            if (region.id == centerId)
            {
                Die();
            }
        }
    }
    public override void Die()
    {
        foreach (var pair in AssetManager.items)
        {
            if (!pair.Value.tags.Contains("tradeable")) return;
            string itemId = pair.Key;
            foreach (Region region in regions)
            {
                region.economy.supply[itemId] = economy.supply[itemId] * (1f/regions.Count);
                region.economy.demand[itemId] = economy.demand[itemId] * (1f/regions.Count);
            }            
        }
        objectManager.DeleteTradeZone(this);
    }
    public void AggregateEconomies()
    {
        foreach (var pair in AssetManager.items)
        {
            if (!pair.Value.tags.Contains("tradeable")) return;

            string itemId = pair.Key;

            economy.supply[itemId] = 0;
            economy.demand[itemId] = 0;

            foreach (Region region in regions)
            {
                economy.supply[itemId] += region.economy.supply[itemId];
                economy.demand[itemId] += region.economy.demand[itemId];
            }            
        }

    }

    public int GetZoneSize()
    {
        return regions.Count;
    }
}