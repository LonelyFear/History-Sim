using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

public class Tectonics
{
    public static float oceanDepth = 0.45f;
    float[,] heightmap;
    int gridSizeX = 2;
    int gridSizeY = 2;
    int ppcx;
    int ppcy;
    TerrainTile[,] tiles;
    List<Vector2I> offshore = new List<Vector2I>();
    List<VoronoiRegion> continentalRegions = new List<VoronoiRegion>();
    List<VoronoiRegion> voronoiRegions = new List<VoronoiRegion>();
    Vector2I worldSize;
    Dictionary<Vector2I, VoronoiRegion> points;
    static Random rng = new Random();

    public float[,] GenerateHeightmap(WorldGeneration w)
    {
        rng = new Random(w.seed);
        worldSize = w.worldSize;
        heightmap = new float[worldSize.X, worldSize.Y];
        tiles = new TerrainTile[worldSize.X, worldSize.Y];
        points = GeneratePoints();
        GenerateRegions(100, 6);
        GenerateContinents();
        AdjustHeightMap();
        GD.Print("Offshore tiles: " + offshore.Count());
        return heightmap;
    }

    public void GenerateContinents()
    {
        while (continentalRegions.Count < Mathf.RoundToInt(voronoiRegions.Count * 0.29f))
        {
            foreach (VoronoiRegion region in continentalRegions.ToArray())
            {
                VoronoiRegion border = region.borderingRegions[rng.Next(0, region.borderingRegions.Count)];
                if (continentalRegions.Count < Mathf.RoundToInt(voronoiRegions.Count * 0.29f))
                {
                    SetRegionContinental(true, border);
                }
            }
        }
    }

    public void AdjustHeightMap()
    {
        FastNoiseLite xNoise = new FastNoiseLite();
        xNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        xNoise.SetFractalOctaves(32);
        xNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        xNoise.SetSeed(rng.Next(-99999, 99999));
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                if (tiles[x, y].region.continental)
                {
                    heightmap[x, y] = Mathf.Clamp(0.6f + (tiles[x, y].coastDist / 100f), 0f, 0.9f);//0.6f + (Mathf.InverseLerp(-0.8f, 1f, xNoise.GetNoise(x / 3f, y / 3f)) * 0.35f);
                }
                if (tiles[x, y].border)
                {
                    heightmap[x, y] = 1f;
                }
            }
        }
        for (int i = 0; i < gridSizeX; i++)
        {
            for (int j = 0; j < gridSizeY; j++)
            {
                Vector2I pos = points[new Vector2I(i, j)].seed;
                heightmap[pos.X, pos.Y] = 1f;
            }
        }
    }
    Dictionary<Vector2I, VoronoiRegion> GeneratePoints()
    {
        ppcx = Mathf.RoundToInt(worldSize.X / (float)gridSizeX);
        ppcy = Mathf.RoundToInt(worldSize.Y / (float)gridSizeY);
        Dictionary<Vector2I, VoronoiRegion> point = new Dictionary<Vector2I, VoronoiRegion>();
        for (int i = 0; i < gridSizeX; i++) {
            for (int j = 0; j < gridSizeY; j++)
            {
                VoronoiRegion region = new VoronoiRegion();
                region.seed = new Vector2I(i * ppcx + rng.Next(0, ppcx), j * ppcy + rng.Next(0, ppcy));
                point.Add(new Vector2I(i, j), region);
                voronoiRegions.Add(region);
            }
        }
        return point;
    }
    public void GenerateRegions(int amount, int landCount)
    {
        int addedLand = 0;
        while (addedLand < landCount)
        {
            VoronoiRegion region = voronoiRegions[rng.Next(0, voronoiRegions.Count)];
            if (!region.continental)
            {
                addedLand += 1;
                SetRegionContinental(true, region);
            }
        }

        // Assigns tiles to their region
        FastNoiseLite xNoise = new FastNoiseLite();
        xNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        xNoise.SetFractalOctaves(8);
        xNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        xNoise.SetSeed(rng.Next(-99999, 99999));
        FastNoiseLite yNoise = new FastNoiseLite();
        yNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        yNoise.SetFractalOctaves(8);
        yNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        yNoise.SetSeed(rng.Next(-99999, 99999));
        float scale = 2;
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                
                TerrainTile tile = new TerrainTile();
                // Domain warping
                int fx = x;//(int)Mathf.PosMod(x + (xNoise.GetNoise(x / scale, y / scale) * 50), worldSize.X);
                int fy = y;//(int)Mathf.PosMod(y + (yNoise.GetNoise(x / scale, y / scale) * 50), worldSize.Y);
                Vector2I pos = new Vector2I(fx, fy);
                VoronoiRegion region = null;
                VoronoiRegion ocean = null;
                float shortestDist = float.PositiveInfinity;
                float shortestOceanDist = float.PositiveInfinity;
                // Loops through the points
                int gx = fx / ppcx;
                int gy = fy / ppcy;
                
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        int gridX = Mathf.PosMod(gx - i, gridSizeX);
                        int gridY = Mathf.PosMod(gy - j, gridSizeY);
                        float dist = pos.WrappedDistanceTo(points[new Vector2I(gridX, gridY)].seed, worldSize);
                        if (dist < shortestDist)
                        {
                            shortestDist = dist;
                            region = points[new Vector2I(gridX, gridY)];
                        }
                    }
                }

                Vector2I midpoint = ocean.seed.WrappedMidpoint(region.seed, worldSize);
                tile.coastDist = pos.WrappedDistanceTo(region.seed, worldSize);            

                tile.region = region;
                tiles[x, y] = tile;

            }
        }

        // Gets Region Borders
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                VoronoiRegion region = tiles[x, y].region;
                Vector2I pos = new Vector2I(x, y);
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, worldSize.X), Mathf.PosMod(pos.Y + dy, worldSize.Y));
                        VoronoiRegion neighbor = tiles[next.X, next.Y].region;
                        if (neighbor != region)
                        {
                            tiles[x, y].border = true;
                            if (!region.borderingRegions.Contains(neighbor))
                            {
                                region.borderingRegions.Add(neighbor);
                            }
                            if (!region.continental)
                            {
                                offshore.Add(pos);
                            }
                        }

                    }
                }
            }
        }
    }

    void SetRegionContinental(bool value, VoronoiRegion region) {

        if (value == true)
        {
            region.continental = true;
            continentalRegions.Add(region);
        }
        else
        {
            region.continental = false;
            continentalRegions.Remove(region);
        }
    }
}
internal class VoronoiRegion
{
    public Vector2I seed;
    public bool continental = false;
    public List<VoronoiRegion> borderingRegions = new List<VoronoiRegion>();
}
internal class TerrainTile
{
    public VoronoiRegion region;
    public float coastDist;
    public bool coastal;
    public bool border;
    public bool offshore;
}
