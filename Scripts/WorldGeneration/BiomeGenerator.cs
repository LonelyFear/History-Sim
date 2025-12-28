using System.Collections.Generic;
using Godot;

public class BiomeGenerator
{
    public string[,] map;
    public string[,] GenerateBiomes(WorldGenerator world)
    {
        map = new string[world.WorldSize.X, world.WorldSize.Y];
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                Biome selectedBiome = AssetManager.GetBiome("ice_sheet");
                float temp = world.GetAverageAnnualTemp(x,y);
                float elevation = world.HeightMap[x, y];
                float moist = world.GetAnnualRainfall(x,y);
                Dictionary<Biome, float> candidates = new Dictionary<Biome, float>();

                foreach (Biome biome in AssetManager.biomes.Values)
                {
                    bool tempInRange = temp >= biome.minTemperature && temp <= biome.maxTemperature;
                    bool moistInRange = moist >= biome.minMoisture && moist <= biome.maxMoisture;

                    if (tempInRange && moistInRange && elevation >= world.SeaLevel * WorldGenerator.WorldHeight)
                    {
                        candidates.Add(biome, 0);
                    }
                    if (elevation < world.SeaLevel * WorldGenerator.WorldHeight)
                    {
                        if (temp <= AssetManager.GetBiome("ice_sheet").maxTemperature)
                        {
                            selectedBiome = AssetManager.GetBiome("ice_sheet");
                            world.HeightMap[x, y] = (int)(world.SeaLevel * WorldGenerator.WorldHeight);
                            break;
                        }
                        else
                        {
                            selectedBiome = AssetManager.GetBiome("ocean");
                            break;
                        }
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

                if (selectedBiome == null)
                {
                    selectedBiome = AssetManager.GetBiome("ice_sheet");
                }
                
                map[x, y] = selectedBiome.id;
            }
        }
        return map;
    }
}