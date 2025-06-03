using Godot;

public class Tile
{
	public long maxPopulation;
	public Biome biome;
	public float fertility;
	public bool hasRoad;
	public Vector2I defaultIcon = new Vector2I();
	public SettlementTypes settlementType = SettlementTypes.NONE;
	public TerrainType terrainType;

	public bool IsLand()
	{
		return terrainType == TerrainType.LAND || terrainType == TerrainType.MOUNTAINS || terrainType == TerrainType.HILLS;
	}
}
public enum TerrainType
{
	LAND,
	WATER,
	ICE,
	HILLS,
	MOUNTAINS,
	
}
public enum SettlementTypes{
	NONE,
	TOWN,
	CITY
}
