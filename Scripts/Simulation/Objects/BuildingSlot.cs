using MessagePack;

[MessagePackObject]
public class BuildingSlot
{
    [Key(0)] public int buildingLevel;
    [Key(1)] public int buildingDamage;
    [Key(2)] public long maxEmployment;
    [Key(3)] public long currentEmployment;
}