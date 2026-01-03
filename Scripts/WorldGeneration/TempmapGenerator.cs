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
    public float[,] GenerateTempMap(WorldGenerator world, out float[,] summerMap, out float[,] winterMap)
    {
        this.world = world;
        map = new float[world.WorldSize.X, world.WorldSize.Y];
        FastNoiseLite noise = new FastNoiseLite(world.rng.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        summerMap = GenerateTempMap(false);
        winterMap = GenerateTempMap(true);
        return map;
    }
    float[,] GenerateTempMap(bool winter)
    {
        map = new float[world.WorldSize.X, world.WorldSize.Y];
        FastNoiseLite noise = new FastNoiseLite(world.rng.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        int divisions = 8;
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = world.WorldSize.X / divisions * (i - 1); x < world.WorldSize.X / divisions * i; x++)
            {
                for (int y = 0; y < world.WorldSize.Y; y++)
                {
                    float latitudeFactor = y / (float)world.WorldSize.Y;
                    float noiseValue = Mathf.InverseLerp(-1, 1, noise.GetNoise(x, y));

                    float noiseWeight = 0.05f;

                    float tempValue = tempCurve.Sample(Mathf.Lerp(latitudeFactor, noiseValue, noiseWeight));
                    float oceanValue = oceanCurve.Sample(Mathf.Lerp(latitudeFactor, noiseValue, noiseWeight));
                    if (winter)
                    {
                        tempValue = tempCurve.Sample(Mathf.Lerp(1 - latitudeFactor, noiseValue, noiseWeight));
                        oceanValue = oceanCurve.Sample(Mathf.Lerp(1 - latitudeFactor, noiseValue, noiseWeight));                    
                    }
                    
                    //map[x, y] = tempValue;
                    float continentiality = CalculateContinentiality(x,y, winter);
                    map[x, y] = Mathf.Lerp(oceanValue, tempValue, continentiality);

                    float heightFactor = 6.5f * (world.HeightMap[x, y]/1000f);
                    if (world.HeightMap[x, y] > 0)
                    {
                        map[x, y] -= heightFactor;
                    }                
                }
            }
        });

        return map;
    }
    public float CalculateContinentiality(int x, int y, bool winter)
    {
        int stepsTaken = 0;

        Vector2 currentPos = new(x,y);
        while (stepsTaken < 50)
        {
            stepsTaken++;
            Vector2 windVel = GetWindVelocity(x, y, winter);
            currentPos -= windVel;
            float sampleX = currentPos.X;
            float sampleY = currentPos.Y;

            Vector2I bottomCorner = new(
                Mathf.PosMod(Mathf.FloorToInt(sampleX), world.WorldSize.X),
                Mathf.PosMod(Mathf.FloorToInt(sampleY), world.WorldSize.Y)
            );

            Vector2I topCorner = new(
                Mathf.PosMod(bottomCorner.X + 1, world.WorldSize.X),
                Mathf.PosMod(bottomCorner.Y + 1, world.WorldSize.Y)
            );

            float tx = sampleX - Mathf.Floor(sampleX);
            float ty = sampleY - Mathf.Floor(sampleY);

            float bottomX = Mathf.Lerp(world.HeightMap[bottomCorner.X, bottomCorner.Y], world.HeightMap[topCorner.X, bottomCorner.Y], tx);
            float topX = Mathf.Lerp(world.HeightMap[bottomCorner.X, topCorner.Y], world.HeightMap[topCorner.X, topCorner.Y],tx);
            float elevation = Mathf.Lerp(bottomX, topX, ty);

            if (elevation < world.SeaLevel * WorldGenerator.WorldHeight)
            {
                break;
            }
        }
        return Mathf.Clamp(continentialityCurve.Sample(stepsTaken), 0, 1);
    }
    Vector2 GetWindVelocity(float x, float y, bool winter)
    {
        float sampleX = x;
        float sampleY = y;

        Vector2I bottomCorner = new(
            Mathf.PosMod(Mathf.FloorToInt(sampleX), world.WorldSize.X),
            Mathf.PosMod(Mathf.FloorToInt(sampleY), world.WorldSize.Y)
        );

        Vector2I topCorner = new(
            Mathf.PosMod(bottomCorner.X + 1, world.WorldSize.X),
            Mathf.PosMod(bottomCorner.Y + 1, world.WorldSize.Y)
        );

        float tx = sampleX - Mathf.Floor(sampleX);
        float ty = sampleY - Mathf.Floor(sampleY);
        Vector2[,] windVelMap = winter ? world.WinterWindVelMap : world.SummerWindVelMap;
        Vector2 bottomX = Vector2.Lerp(windVelMap[bottomCorner.X, bottomCorner.Y], windVelMap[topCorner.X, bottomCorner.Y], tx);
        Vector2 topX = Vector2.Lerp(windVelMap[bottomCorner.X, topCorner.Y], windVelMap[topCorner.X, topCorner.Y], tx);  
        return Vector2.Lerp(bottomX, topX, ty);      
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
