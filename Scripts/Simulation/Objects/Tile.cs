using Godot;
using MessagePack;

[MessagePackObject(keyAsPropertyName: true)]
public class Tile
{
	public long maxPopulation;
	public Biome biome;
	public float moisture;
	public float temperature;
	public float elevation;
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
