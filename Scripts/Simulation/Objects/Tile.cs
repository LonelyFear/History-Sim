using Godot;

public class Tile
{
	public long maxPopulation;
	public Biome biome;
	public float fertility;
	public bool hasRoad;
	public Vector2I defaultIcon = new Vector2I();
	public SettlementTypes settlementType = SettlementTypes.NONE;
	public Biome.TerrainType terrainType;
}
public enum SettlementTypes{
	NONE,
	TOWN,
	CITY
}
