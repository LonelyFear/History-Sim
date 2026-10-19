using System.Collections.Generic;
using Godot;
using MessagePack;
[MessagePackObject(AllowPrivate = true)]
public partial class TradeZone : NamedObject
{
    [Key(7)] ulong centerId { get; set; }
    [Key(8)] public ulong[] regionIds { get; set; }
    [IgnoreMember] public HashSet<Region> regions { get; set; } = [];
    [Key(9)] public Color color { get; set; }
    [Key(10)]  public ulong? controllerId { get; set; } = null;
    [Key(11)] public float totalMarketWeight = 1f; 
       
    [IgnoreMember] Region _center;
    [IgnoreMember] public Region center
    {
        get
        {
            _center ??= ObjectManager.GetRegion(centerId);
            return _center;
        } 
        set
        {
            centerId = value.id;
            _center = value;
        }         
    }

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
        originalTradeZone?.RemoveRegion(region);
        
        region.tradeZone = this;
        regions.Add(region);    

        if (originalTradeZone?.center == region)
        {
            foreach (Region r in originalTradeZone.regions)
            {
                AddRegion(r);
            }
        }

    }
    public void RemoveRegion(Region region)
    {
        if (regions.Remove(region))
        {
            region.tradeZone = null;
            if (region == center)
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