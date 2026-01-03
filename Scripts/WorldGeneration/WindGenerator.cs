using Godot;
using System.Linq;
using System.Threading.Tasks;
using Vector2 = System.Numerics.Vector2;
public class WindGenerator()
{
    Vector2I worldSize;
    Curve prevailingWindCurve = GD.Load<Curve>("res://Curves/PrevailingWindCurve.tres");
    Curve windSpeedCurve = GD.Load<Curve>("res://Curves/WindSpeedCurve.tres");
    WorldGenerator world;
    
    public void GeneratePrevailingWinds(WorldGenerator world)
    {
        this.world = world;
        worldSize = world.WorldSize;      
        GeneratePrevailingWinds(false);
        GeneratePrevailingWinds(true);
    }
    void GeneratePrevailingWinds(bool winter)
    {
        Vector2[,] windVectorMap = new Vector2[worldSize.X, worldSize.Y];
        float[,] windDirMap = new float[worldSize.X, worldSize.Y];
        float[,] windSpeedMap = new float[worldSize.X, worldSize.Y];

        int[,] heightMap = new int[worldSize.X, worldSize.Y];
        float[,] coastDistMap = new float[worldSize.X, worldSize.Y];

        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                heightMap[x,y] = world.cells[x,y].elevation;
                coastDistMap[x,y] = world.cells[x,y].coastDist;
            }
        }
        FastNoiseLite noise = new FastNoiseLite(world.rng.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        int divisions = 8;
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = world.WorldSize.X / divisions * (i - 1); x < world.WorldSize.X / divisions * i; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    float posY = y;
                    float noiseValue = Mathf.InverseLerp(-1, 1, noise.GetNoise(x,y));
                    float normalizedY = posY / world.WorldSize.Y;
                    float latitudeFactor = Mathf.Abs(normalizedY - 0.5f) * 2f;

                    float noiseStrength = 0.1f;
                    float dir = prevailingWindCurve.Sample(Mathf.Lerp(latitudeFactor, noiseValue, noiseStrength));

                    windDirMap[x, y] = dir;
                    if (normalizedY < 0.5f)
                    {
                        windDirMap[x, y] = Mathf.PosMod(180f - dir, 360f);
                    }
                    
                    float windMaxSpeed = 12f;

                    float speedSampleValue = Mathf.Lerp(normalizedY, noiseValue, noiseStrength);

                    windSpeedMap[x, y] = windMaxSpeed * windSpeedCurve.Sample(speedSampleValue);
                    if (winter)
                    {
                        windSpeedMap[x, y] = windMaxSpeed * windSpeedCurve.Sample(1f - speedSampleValue);
                    }

                    if (heightMap[x, y] > world.SeaLevel * WorldGenerator.WorldHeight)
                    {
                        // Land Winds
                        windSpeedMap[x, y] *= 0.5f;
                        Vector2 windVector = GetVector(windSpeedMap[x,y], windDirMap[x,y]);
                        Vector2 terrainGradient = -Utility.GetGradient(heightMap, x,y) * 0.01f;

                        windVector += terrainGradient;    

                        windSpeedMap[x, y] = windVector.Length();
                        windDirMap[x,y] = GetBearing(windVector);
                    } else
                    {
                        // Ocean Winds
                        Vector2 windVector = GetVector(windSpeedMap[x,y], windDirMap[x,y]);
                        Vector2 coastGradient = Utility.GetGradient(coastDistMap, x,y);
                        float windSpeed = windSpeedMap[x, y];
                        float dot = Vector2.Dot(windVector, coastGradient);
                        if (dot < 0)
                        {
                            // Wind blowing towards coast
                            windVector = Vector2.Lerp(Vector2.Normalize(windVector), Vector2.Normalize(coastGradient), 0.5f) * windSpeed;
                        }
                    }
                }
            }
        });
        // Blurs wind map
        for (int i = 0; i < 4; i++)
        {
            float[,] newWindSpeedMap = new float[worldSize.X, worldSize.Y];
            float[,] newWindDirMap = new float[worldSize.X, worldSize.Y];
            for (int x = 0; x < worldSize.X; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            Vector2I testPos = new Vector2I(Mathf.PosMod(x + dx, world.WorldSize.X), Mathf.PosMod(y + dy, world.WorldSize.Y));
                            newWindSpeedMap[x,y] = Mathf.Lerp(windSpeedMap[x,y], windSpeedMap[testPos.X, testPos.Y], 0.5f);
                            newWindDirMap[x,y] = Mathf.Lerp(windDirMap[x,y], windDirMap[testPos.X, testPos.Y], 0.5f);
                        }
                    }
                }
            }
            windSpeedMap = newWindSpeedMap;
            windDirMap = newWindDirMap;
        }

        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                float bearing = windDirMap[x,y];
                float speed = windSpeedMap[x,y];

                if (winter)
                {
                    world.cells[x,y].januaryWindVel = GetVector(speed, bearing);
                } else
                {
                    world.cells[x,y].julyWindVel = GetVector(speed, bearing);
                }
            }
        }
    }
    public Vector2 GetVector(float speed, float bearing)
    {
        //return new(speed,0);
        float rad = Mathf.DegToRad(bearing);
        return new Vector2(speed * Mathf.Sin(rad), speed * Mathf.Cos(rad));
    }
    public float GetBearing(Vector2 vector)
    {
        float rad = Mathf.Atan2(vector.X, vector.Y);
        return Mathf.RadToDeg(rad);
    }
}