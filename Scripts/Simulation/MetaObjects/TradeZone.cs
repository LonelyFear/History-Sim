using System;
using System.Collections.Generic;
using Godot;

public class TradeZone
{
    public Region CoT;
    List<Region> regions = new List<Region>();
    public Color color;
    static Random rng = new Random();
    public TradeZone(Region region)
    {
        color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle());
        CoT = region;
        regions = [CoT];
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