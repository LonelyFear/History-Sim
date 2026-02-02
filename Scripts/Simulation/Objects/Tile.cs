using Godot;
using MessagePack;

[MessagePackObject(keyAsPropertyName: true)]
public class Tile : Cell
{
	public Vector2I pos;
	public Biome biome;
	public float navigability;
	public float arability;
	public float survivalbility;
	public bool coastal;
	public bool renderOverlay = true;
	public SettlementTypes settlementType = SettlementTypes.NONE;
	public TerrainType terrainType;

	public Tile() {}
	public Tile(Cell cell)
	{
		januaryPET = cell.januaryPET;
		julyPET = cell.julyPET;
		januaryRainfall = cell.januaryRainfall;
		julyRainfall = cell.julyRainfall;
		januaryTemp = cell.januaryTemp;
		julyTemp = cell.julyTemp;
		januaryWindVel = cell.januaryWindVel;
		julyWindVel = cell.julyWindVel;
		elevation = cell.elevation;
		julyDaylight = cell.julyDaylight;
		januaryDaylight = cell.januaryDaylight;
		continentiality = cell.continentiality;

		biome = AssetManager.GetBiome(cell.biomeId);
		
		arability = biome.arability;
		navigability = biome.navigability;
		survivalbility = biome.survivability;

		switch (biome.type)
		{
			case "land":
				terrainType = TerrainType.LAND;
				break;
			case "water":
				if (biome.id == "river")
				{
					terrainType = TerrainType.RIVER;
					break;
				}

				renderOverlay = false;
				terrainType = TerrainType.DEEP_WATER;
				if (elevation > -800f)
				{
					terrainType = TerrainType.SHALLOW_WATER;
				}
				break;
			default:
				terrainType = TerrainType.ICE;
				break;
		}

		if (terrainType == TerrainType.LAND)
		{
			if (elevation > WorldGenerator.MountainThreshold)
			{
				navigability *= 0.1f;
				arability *= 0.25f;
				survivalbility *= 0.8f;
				terrainType = TerrainType.MOUNTAINS;
			}
			else if (elevation > WorldGenerator.HillThreshold)
			{
				navigability *= 0.25f;
				arability *= 0.5f;
				terrainType = TerrainType.HILLS;
			}
		}
	}
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
