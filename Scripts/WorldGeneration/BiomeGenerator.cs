using System.Collections.Generic;
using Godot;

public class BiomeGenerator
{
    public Biome[,] map;
    public Biome[,] GenerateBiomes()
    {
        map = new Biome[WorldGenerator.WorldSize.X, WorldGenerator.WorldSize.Y];
        for (int x = 0; x < WorldGenerator.WorldSize.X; x++)
        {
            for (int y = 0; y < WorldGenerator.WorldSize.Y; y++)
            {
                Biome selectedBiome = AssetManager.GetBiome("ice_sheet");
                float temp = WorldGenerator.GetUnitTemp(WorldGenerator.TempMap[x, y]);
                float elevation = WorldGenerator.HeightMap[x, y];
                float moist = WorldGenerator.GetUnitRainfall(WorldGenerator.RainfallMap[x, y]);
                Dictionary<Biome, float> candidates = new Dictionary<Biome, float>();

                foreach (Biome biome in AssetManager.biomes.Values)
                {
                    bool tempInRange = temp >= biome.minTemperature && temp <= biome.maxTemperature;
                    bool moistInRange = moist >= biome.minMoisture && moist <= biome.maxMoisture;

                    if (tempInRange && moistInRange && elevation >= WorldGenerator.SeaLevel)
                    {
                        candidates.Add(biome, 0);
                    }
                    if (elevation < WorldGenerator.SeaLevel)
                    {
                        selectedBiome = AssetManager.GetBiome("ocean");
                    }

                }
                float minTRange = float.PositiveInfinity;
                float minMRange = float.PositiveInfinity;
                if (candidates.Count > 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        foreach (Biome biome in candidates.Keys)
                        {
                            if (minTRange > biome.maxTemperature - biome.minTemperature)
                            {
                                minTRange = biome.maxTemperature - biome.minTemperature;
                                selectedBiome = biome;
                            }
                            if (minMRange > biome.maxMoisture - biome.minMoisture)
                            {
                                minMRange = biome.maxMoisture - biome.minMoisture;
                                selectedBiome = biome;
                            }
                        }
                    }

                }
                
                // if (WorldGenerator.HydroMap[x, y] > 11f && elevation >= WorldGenerator.SeaLevel)
                // {
                //     selectedBiome = AssetManager.GetBiome("river");
                // }
                
                map[x, y] = selectedBiome;
            }
        }
        return map;
    }
}