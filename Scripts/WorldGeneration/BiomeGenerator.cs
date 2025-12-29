using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public class BiomeGenerator
{
    public string[,] map;
    public List<string>[,] plantTypes;
    WorldGenerator world;
    public List<string>[,] GeneratePlantTypes()
    {
        List<string>[,] output = new List<string>[world.WorldSize.X, world.WorldSize.Y];
        int divisions = 8;

        PlantType[] plantTypes = [.. AssetManager.plantTypes.OrderBy(type => type.dominance)];
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = world.WorldSize.X / divisions * (i - 1); x < world.WorldSize.X / divisions * i; x++)
            {
                for (int y = 0; y < world.WorldSize.Y; y++)
                {
                    float[] tempValues = [world.GetTempForMonth(x,y,0), world.GetTempForMonth(x,y,6)];
                    float[] aValues = [CalcA(x,y,0), CalcA(x,y,6)];
                    int currentDominance = int.MaxValue;
                    float a = aValues.Sum() / aValues.Length;

                    foreach (PlantType plantType in plantTypes)
                    {
                        bool addedPlant = false;
                        if (plantType.dominance > currentDominance)
                        {
                            continue;
                        }

                        addedPlant = tempValues.Min() >= plantType.minColdTemp;
                        if (!addedPlant) continue;
                        addedPlant = tempValues.Min() <= plantType.maxColdTemp;
                        if (!addedPlant) continue;
                        addedPlant = GetGDD(x,y) >= plantType.minGDD;
                        if (!addedPlant) continue;
                        addedPlant = GetGDD(x,y,true) >= plantType.minGDDz;
                        if (!addedPlant) continue;
                        addedPlant = tempValues.Max() >= plantType.minWarmTemp;
                        if (!addedPlant) continue;
                        addedPlant = a >= plantType.minA;
                        if (!addedPlant) continue;
                        addedPlant = a <= plantType.maxA;
                        if (!addedPlant) continue;

                        if (addedPlant)
                        {
                            currentDominance = plantType.dominance;
                            if (output[x,y] == null)
                            {
                            output[x,y] = []; 
                            }
                            //GD.Print(plantType.id);
                            output[x,y].Add(plantType.id);
                        }
                    }
                }
            }
        });     
        return output;
    }
    public float CalcA(int x, int y, int month)
    {
        float PET = world.GetPETForMonth(x, y, month);
        if (PET <= 0)
        {
            return 10;
        }
        float a = world.GetRainfallForMonth(x,y, month) / PET;
        return a;
    }
    public int GetGDD(int x, int y, bool zero = false)
    {
        float baseTemp = zero ? 0 : 5;
        int GDD = 0;
        for (int i = 0; i < 12; i++)
        {
            float temp = world.GetTempForMonth(x,y,i);
            GDD += (int)(Mathf.Max(temp - baseTemp, 0) * 30) ;
        }
        return GDD; 
    }
    public string[,] GenerateBiomes(WorldGenerator world, bool useBIOME = false)
    {
        this.world = world;

        if (useBIOME)
        {
            plantTypes = GeneratePlantTypes();
        }

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
                    if (useBIOME)
                    {
                        //if (plantTypes[x,y] != null) GD.Print(plantTypes[x,y].ToString());
                        if (elevation < world.SeaLevel * WorldGenerator.WorldHeight)
                        {
                            selectedBiome = AssetManager.GetBiome("ocean");
                            /*
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
                            */                            
                        }
                        else if (plantTypes[x,y] != null && plantTypes[x,y].OrderBy(s => s).SequenceEqual(biome.plantTypes.OrderBy(s => s)))
                        {
                            selectedBiome = biome;
                        }
                        continue;
                    }
                    bool tempInRange = temp >= biome.minTemperature && temp <= biome.maxTemperature;
                    bool moistInRange = moist >= biome.minMoisture && moist <= biome.maxMoisture;

                    if (tempInRange && moistInRange && elevation >= world.SeaLevel * WorldGenerator.WorldHeight)
                    {
                        candidates.Add(biome, 0);
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