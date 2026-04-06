using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public class BiomeGenerator
{
    public string[,] map;
    WorldGenerator world;
    public float CalcA(int x, int y, int month)
    {
        float PET = world.cells[x,y].GetPETForMonth(month);
        if (PET <= 0)
        {
            return 10;
        }
        float a = world.cells[x,y].GetRainfallForMonth(month) / PET;
        return a;
    }
    public int GetGDD(int x, int y, bool zero = false)
    {
        float baseTemp = zero ? 0 : 5;
        int GDD = 0;
        for (int i = 0; i < 12; i++)
        {
            float temp = world.cells[x,y].GetTempForMonth(i);
            GDD += (int)(Mathf.Max(temp - baseTemp, 0) * 30) ;
        }
        return GDD; 
    }
    public void GenerateBiomes(WorldGenerator world)
    {
        this.world = world;

        map = new string[world.WorldSize.X, world.WorldSize.Y];

        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                Biome selectedBiome = AssetManager.GetBiome("ice_sheet");
                float temp = world.cells[x,y].GetAverageTemp();
                int elevation = world.cells[x, y].elevation;
                float seaLevel = world.SeaLevel * WorldGenerator.WorldHeight;
                float moist = world.cells[x,y].GetAnnualRainfall();
                Dictionary<Biome, float> candidates = [];

                foreach (Biome biome in AssetManager.biomes.Values)
                {
                    bool tempInRange = temp >= biome.minTemperature && temp <= biome.maxTemperature;
                    bool moistInRange = moist >= biome.minMoisture && moist <= biome.maxMoisture;

                    if (tempInRange && moistInRange && elevation >= 0)
                    {
                        candidates.Add(biome, 0);
                    }
                    else if (elevation < 0)
                    {
                        selectedBiome = AssetManager.GetBiome("ocean");
                        if (temp <= AssetManager.GetBiome("ice_sheet").maxTemperature)
                        {
                            selectedBiome = AssetManager.GetBiome("ice_sheet");
                            world.cells[x, y].elevation = 0;
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
                
                world.cells[x,y].biomeId = selectedBiome.id;
            }
        }
    }
}