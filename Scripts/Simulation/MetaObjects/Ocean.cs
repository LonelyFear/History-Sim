using System.Collections.Generic;
using MessagePack;
using Godot;
using System.Linq;

[MessagePackObject(AllowPrivate = true)]
public partial class Ocean : NamedObject
{
    [IgnoreMember] public HashSet<Region> waterRegions = [];
    [IgnoreMember] public HashSet<Region> coastalRegions = [];
    [Key(8)] List<ulong> waterRegionIds;
    [Key(9)] List<ulong> coastalRegionIds;
    [Key(10)] public Color color;

    public override void PrepareForSave()
    {
        base.PrepareForSave();
        coastalRegionIds = [..coastalRegions.Select(r => r.id)];
        waterRegionIds = [..waterRegions.Select(r => r.id)];
    }
    public override void LoadFromSave()
    {
        base.LoadFromSave();
        coastalRegions = [..coastalRegionIds.Select(r => ObjectManager.GetRegion(r))];
        waterRegions = [..waterRegionIds.Select(r => ObjectManager.GetRegion(r))];
    }
}