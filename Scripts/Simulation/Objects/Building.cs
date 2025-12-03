public class Building
{
    public string id {get; set;} = "building";
    public string name {get; set;} = "Building";
    public int workersPerLevel {get; set;}
    public SocialClass profession {get; set;}
    public Settlement.Tier minimumTier = Settlement.Tier.VILLAGE;
    public int baseCost {get; set;}
}

public enum BuildingType
{
    RESOURCE,
    PRODUCTION,
    MILITARY,
    HABITATION,
}