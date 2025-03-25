public class Tile
{
	public long maxPopulation;
    public Biome biome;
    public bool hasRoad;
    public SettlementTypes settlementType = SettlementTypes.NONE;
   
}
public enum SettlementTypes{
	NONE,
	FARMS,
	TOWN,
	CITY
}
