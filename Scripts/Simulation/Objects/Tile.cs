using Godot;
using MessagePack;

[MessagePackObject]
public class Tile : Cell
{
	[Key(16)] public ulong? regionId;
	[Key(17)] public Vector2I pos;
	[Key(18)] public float navigability;
	[Key(19)] public float arability;
	[Key(20)] public float survivalbility;
	[Key(21)] public bool coastal;
	[Key(22)] public bool renderOverlay = true;
	[Key(23)] public SettlementTypes settlementType = SettlementTypes.NONE;
	[Key(24)] public TerrainType terrainType;

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
		biomeId = cell.biomeId;
		
		arability = GetBiome().arability;
		navigability = GetBiome().navigability;
		survivalbility = GetBiome().survivability;

		switch (GetBiome().type)
		{
			case "land":
				terrainType = TerrainType.LAND;
				break;
			case "water":
				if (biomeId == "river")
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
	public Biome GetBiome()
	{
		return AssetManager.GetBiome(biomeId);
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
