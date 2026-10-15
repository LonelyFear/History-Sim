using Godot;

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
    public const int MinVassalizationSize = 3;

    // Tech
    public const float BaseRedTechSeedChance = 0.008f;
    public const float BaseBlueTechSeedChance = 0.0005f;
    public const float BlueTechBonus = 0.0002f;
    public const float BaseIndTechSeedChance = 0.01f;
    public const float BaseRedTechSpreadChance = 0.005f;
    public const float BaseIndTechSpreadChance = 0.005f;
    public const float BlueTechSpreadChance = 0.0005f;

    // States
    public const float RandomStateFormationChance = 0.001f;
    
    // Cultures
    public const float MinCultureDifference = 0.1f;
    public const float BaseCultureDriftChance = 0.003f;
    public const float BaseRegionDriftChance = 0.001f;
    public const float BaseStateDriftChance = 0.1f;
}