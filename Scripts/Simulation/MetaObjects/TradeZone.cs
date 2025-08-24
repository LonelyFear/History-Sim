using System;
using System.Collections.Generic;
using Godot;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true, AllowPrivate = true)]
public class TradeZone
{
    [IgnoreMember]
    public Region CoT { get; set; }
    [IgnoreMember]
    public List<Region> regions { get; set; } = new List<Region>();
    public Color color { get; set; }
    [IgnoreMember]
    static Random rng = new Random();

    public TradeZone CreateZone(Region region)
    {
        color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
        CoT = region;
        regions = [CoT];
        return this;
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
                DestroyZone();
            }
        }
    }
    public void DestroyZone()
    {
        foreach (Region region in regions.ToArray())
        {
            RemoveRegion(region);
        }
    }

    public int GetZoneSize()
    {
        return regions.Count;
    }
}