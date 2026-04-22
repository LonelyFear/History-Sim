using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MessagePack.Formatters;

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
                string selectedId = "ice_sheet";

                float temp = world.cells[x,y].GetAverageTemp();
                int elevation = world.cells[x, y].elevation;
                float moist = world.cells[x,y].GetAnnualRainfall();

                List<string> candidates = [];

                foreach (var pair in AssetManager.biomes)
                {
                    Biome biome = pair.Value;
                    bool tempInRange = temp >= biome.minTemperature && temp <= biome.maxTemperature;
                    bool moistInRange = moist >= biome.minMoisture && moist <= biome.maxMoisture;
                    bool elevInRange = elevation >= biome.minElevation && elevation <= biome.maxElevation;
                    if (tempInRange && moistInRange && elevInRange)
                    {
                        candidates.Add(pair.Key);
                    }
                    /*
                    else if (elevation < 0)
                    {
                        selectedId = "ocean";
                        if (temp <= AssetManager.GetBiome("ice_sheet").maxTemperature)
                        {
                            selectedId = "ice_sheet";
                            world.cells[x, y].elevation = 1;
                            break;
                        }                        
                    }
                    */
                }

                float minTRange = float.PositiveInfinity;
                float minMRange = float.PositiveInfinity;
                float minERange = float.PositiveInfinity;
                if (candidates.Count > 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        foreach (string biomeId in candidates)
                        {
                            Biome biome = AssetManager.GetBiome(biomeId);
                            if (minTRange > Mathf.Abs(biome.maxTemperature - biome.minTemperature))
                            {
                                minTRange = Mathf.Abs(biome.maxTemperature - biome.minTemperature);
                                selectedId = biomeId;
                            }
                            if (minMRange > Mathf.Abs(biome.maxMoisture - biome.minMoisture))
                            {
                                minMRange = Mathf.Abs(biome.maxMoisture - biome.minMoisture);
                                selectedId = biomeId;
                            }
                            if (minERange > Mathf.Abs(biome.maxElevation - biome.minElevation))
                            {
                                minERange = Mathf.Abs(biome.maxElevation - biome.minElevation);
                                selectedId = biomeId;
                            }
                        }
                    }
                }
                
                world.cells[x,y].biomeId = selectedId;
            }
        }
    }
}