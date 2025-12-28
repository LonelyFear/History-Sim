using Godot;
using MessagePack;

[MessagePackObject(keyAsPropertyName: true)]
public class Tile
{
	public long maxPopulation;
	public Biome biome;
	public float navigability;
	public float arability;
	public float survivalbility;
	public bool coastal;
	public bool renderOverlay = true;
	public Vector2I defaultIcon = new Vector2I();
	public SettlementTypes settlementType = SettlementTypes.NONE;
	public TerrainType terrainType;

	public bool IsLand()
	{
		return terrainType == TerrainType.LAND || terrainType == TerrainType.MOUNTAINS || terrainType == TerrainType.HILLS;
	}
	public bool IsWater()
	{
		return terrainType == TerrainType.DEEP_WATER || terrainType == TerrainType.SHALLOW_WATER || terrainType == TerrainType.RIVER;
	}
	public bool IsOcean()
	{
		return terrainType == TerrainType.DEEP_WATER || terrainType == TerrainType.SHALLOW_WATER;
	}
}
public enum TerrainType
{
	ICE,
	DEEP_WATER,
	SHALLOW_WATER,
	RIVER,
	LAND,
	HILLS,
	MOUNTAINS,
	
}
public enum SettlementTypes{
	NONE,
	TOWN,
	CITY
}
