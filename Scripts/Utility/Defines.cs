public static class Defines
{
    public enum GoodsType
    {
        NONE,
        FOOD,
        TOOLS,
        LUXURY,
        WEAPONS,
    }
    public enum BiomeType
    {
        ICE,
        WATER,
        LAND
    }

    public enum BuildingTechScale
    {
        NONE,
        SOCIETY,
        MILITARY,
        SCIENCE,
        AVERAGE
    }

    public enum BuildingType
    {
        INDUSTRY,
        CIVIL,
        MILITARY,
        GOVERNMENT
    }
    // Wars
    public const int TruceLengthYears = 10;
    public const float CivilWarStabilityGain = 0.3f;
}