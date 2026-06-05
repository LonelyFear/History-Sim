using System.Collections.Generic;
using MessagePack;
using Godot;

[MessagePackObject]
public class Ocean : NamedObject
{
    [IgnoreMember] public HashSet<Region> waterRegions = [];
    [IgnoreMember] public HashSet<Region> coastalRegions = [];
    [Key(8)] public List<long> regionIds;
    [Key(9)] public Color color;
}