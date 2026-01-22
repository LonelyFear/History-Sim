using Godot;
using System.Linq;
using System.Threading.Tasks;
using Vector2 = System.Numerics.Vector2;
public class WindGenerator()
{
    Vector2I worldSize;
    Curve prevailingWindCurve = GD.Load<Curve>("res://Curves/WindBearingCurve.tres");
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
                heightMap[x,y] = Mathf.Max(world.cells[x,y].elevation,0);
                coastDistMap[x,y] = world.cells[x,y].coastDist;
            }
        }
        FastNoiseLite noise = new FastNoiseLite(world.WindNoiseSeed);
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
                    float nx = winter ? x : x + (worldSize.X * 2);

                    float noiseValue = Mathf.InverseLerp(-1, 1, noise.GetNoise(nx * 0.3f,0)) * 1f;

                    if (winter) world.cells[x,y].januaryWindOffset = noiseValue;
                    else world.cells[x,y].julyWindOffset = noiseValue;

                    float normalizedY = y / (float) world.WorldSize.Y;
                    float noiseStrength = 0.1f;
                    float sampleValue = winter ? Mathf.Lerp(normalizedY, noiseValue, noiseStrength) : 1f - Mathf.Lerp(normalizedY, noiseValue, noiseStrength);

                    float dir = prevailingWindCurve.Sample(sampleValue);
                    if (!winter)
                    {
                        dir = Mathf.PosMod(180f - dir, 360f);
                    }
                    windDirMap[x, y] = dir;

                    float windMaxSpeed = 12f;
                    windSpeedMap[x, y] = windMaxSpeed * windSpeedCurve.Sample(sampleValue);
                    Vector2 windVector = GetVector(windSpeedMap[x,y], windDirMap[x,y]);
                    if (heightMap[x, y] > 0)
                    {
                        // Land Winds
                        windSpeedMap[x, y] *= 0.5f;
                        
                        Vector2 terrainGradient = -Utility.GetGradient(heightMap, x,y) * 0.01f;

                        windVector += terrainGradient;    
                    }

                    windSpeedMap[x, y] = windVector.Length();
                    windDirMap[x,y] = GetBearing(windVector);
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