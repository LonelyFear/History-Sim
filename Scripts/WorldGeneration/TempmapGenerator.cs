using Godot;
using System.Threading.Tasks;
using Vector2 = System.Numerics.Vector2;
public class TempmapGenerator
{
    Curve tempCurve = GD.Load<Curve>("res://Curves/Climate/SeasonalTempCurve.tres");
    Curve oceanCurve = GD.Load<Curve>("res://Curves/Climate/SeasonalOceanicTempCurve.tres");
    Curve continentialityCurve = GD.Load<Curve>("res://Curves/ContinentialityCurve.tres");
    WorldGenerator world;
    float[,] map;
    float[,] heightmap;
    public void GenerateTempMap(WorldGenerator world)
    {
        this.world = world;
        FastNoiseLite noise = new FastNoiseLite(world.rng.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        GenerateTempMap(false);
        GenerateTempMap(true);
    }
    void GenerateTempMap(bool winter)
    {
        map = new float[world.WorldSize.X, world.WorldSize.Y];
        heightmap = new float[world.WorldSize.X, world.WorldSize.Y];

        FastNoiseLite noise = new FastNoiseLite(world.rng.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        int divisions = 8;
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                heightmap[x,y] = world.cells[x,y].elevation;
            }
        }
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = world.WorldSize.X / divisions * (i - 1); x < world.WorldSize.X / divisions * i; x++)
            {
                for (int y = 0; y < world.WorldSize.Y; y++)
                {
                    float yNormalized = y / (float)world.WorldSize.Y;
                    float latitudeFactor = winter ? 1f - yNormalized : yNormalized;
                    float noiseValue = Mathf.InverseLerp(-1, 1, noise.GetNoise(x, y));

                    float noiseWeight = 0.05f;

                    float tempValue = tempCurve.Sample(Mathf.Lerp(latitudeFactor, noiseValue, noiseWeight));
                    float oceanValue = oceanCurve.Sample(Mathf.Lerp(latitudeFactor, noiseValue, noiseWeight));
                    
                    //map[x, y] = tempValue;
                    float continentiality = CalculateContinentiality(x,y, winter);
                    world.cells[x,y].continentiality = continentiality;
                    map[x, y] = Mathf.Lerp(oceanValue, tempValue, continentiality);

                    float heightFactor = 6.5f * (world.cells[x,y].elevation/1000f);
                    if (world.cells[x,y].elevation > 0)
                    {
                        map[x, y] -= heightFactor;
                    }                
                }
            }
        });
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                if (winter)
                {
                    world.cells[x,y].januaryTemp = (int)map[x,y];
                } else
                {
                    world.cells[x,y].julyTemp = (int)map[x,y];
                }
            }
        } 
    }
    public float CalculateContinentiality(int x, int y, bool winter)
    {
        int stepsTaken = 0;

        Vector2 currentPos = new(x,y);
        while (stepsTaken < 50)
        {
            float elevation = Utility.BilinearInterpolation(heightmap, Mathf.PosMod(currentPos.X, world.WorldSize.X), Mathf.PosMod(currentPos.Y, world.WorldSize.Y));
            if (elevation < 0)
            {
                break;
            }      

            stepsTaken++;
            Vector2 windVel = GetWindVelocity(currentPos.X, currentPos.Y, winter);
            currentPos -= windVel;
        }
        return Mathf.Clamp(continentialityCurve.Sample(Mathf.Min(stepsTaken, 50)), 0, 1);
    }
    Vector2 GetWindVelocity(float x, float y, bool winter)
    {
        float sampleX = x;
        float sampleY = y;

        float tx = sampleX - Mathf.Floor(sampleX);
        float ty = sampleY - Mathf.Floor(sampleY);

        int x0 = Mathf.PosMod((int)x, world.WorldSize.X);
        int x1 = Mathf.PosMod(x0 + 1, world.WorldSize.X);
        int y0 = Mathf.PosMod((int)y, world.WorldSize.Y);
        int y1 = Mathf.PosMod(y0 + 1, world.WorldSize.Y);    

        Vector2 bottomX = Vector2.Lerp(GetWindVel(x0, y0, winter), GetWindVel(x1, y0, winter), tx);
        Vector2 topX = Vector2.Lerp(GetWindVel(x0, y1, winter), GetWindVel(x1, y1, winter), tx);  
        return Vector2.Lerp(bottomX, topX, ty);      
    }
    Vector2 GetWindVel(int x, int y, bool winter)
    {
        if (winter)
        {
            return world.cells[x,y].januaryWindVel;
        } else
        {
            return world.cells[x,y].julyWindVel;
        }
    }
    /*
    public float[,] GenerateTempMap(float scale, WorldGenerator world){
        map = new float[world.WorldSize.X, world.WorldSize.Y];
        FastNoiseLite noise = new FastNoiseLite(world.rng.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        float averageTemp = 0;
        //float[,] falloff = Falloff.GenerateFalloffMap(world.WorldSize.X, world.WorldSize.Y, false, 1, 1.1f);
        GD.Print("Equator Temp: " + (1f - (Mathf.Abs(50 - (world.WorldSize.Y / 2f)) / (world.WorldSize.Y / 2f))));
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                float latitudeFactor = 1f - (Mathf.Abs(y - (world.WorldSize.Y / 2f)) / (world.WorldSize.Y / 2f));
                float noiseValue = Mathf.InverseLerp(-1, 1, noise.GetNoise(x / scale, y / scale));
                
                map[x,y] = tempCurve.Sample(Mathf.Lerp(latitudeFactor, noiseValue, 0.15f));

                float heightFactor = 6.5f * (world.HeightMap[x, y]/1000f);
                if (world.HeightMap[x, y] > 0)
                {
                    map[x, y] -= heightFactor;
                }

                //map[x, y] = Mathf.Clamp(map[x,y], 0, 1);
                averageTemp += map[x, y];
            }
        }
        GD.Print((averageTemp/(world.WorldSize.X*world.WorldSize.Y)).ToString("Average: 0.0") + " C");
        return map;
    } 
    */
}
