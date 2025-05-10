using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class Tectonics
{
    WorldGeneration world;
    Sprite2D textureDisplay;
    public static float oceanDepth = 0.45f;
    Image image;
    Plate[] plates;
    Crust[] crusts;
    TectonicTile[] tiles;
    Vector2I worldSize;
    int seed;
    float seaLevel;
    int sizeMult;
    float maxPressure = float.MinValue;
    float minPressure = float.MaxValue;
    float maxElevation = float.MinValue;
    Random rng;
    bool advancedGeneration;

    public float[,] RunSimulation(WorldGeneration w, int plateCount, bool advanced = false, Sprite2D display = null){
        InitSim(w, plateCount, advanced, display);
        if (advancedGeneration){
            crusts = null;
            SimStep();
        } else {
            foreach (Crust crust in crusts){
                GetPressure(crust);
                world.heightMapProgress += 0.5f;
            }
            Parallel.ForEach(crusts, GrowRange);              
        }
        return GetElevations();
    }
    float[,] GetElevations(){
        float[,] elevations = new float[worldSize.X, worldSize.Y];
        foreach (Crust crust in crusts){
            elevations[crust.pos.X, crust.pos.Y] = crust.elevation;
        }
        return elevations;
    }

    public void InitSim(WorldGeneration w, int plateCount, bool advanced = false, Sprite2D display = null){
        textureDisplay = display;
        advancedGeneration = advanced;
        world = w;
        worldSize = world.worldSize;
        seaLevel = world.seaLevel;
        seed = world.seed;
        rng = new Random(seed);
        image = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgb8);

        crusts = new Crust[worldSize.X * worldSize.Y];
        tiles = new TectonicTile[worldSize.X * worldSize.Y];
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                TectonicTile segment = new TectonicTile(){
                    pos = new Vector2I(x,y),
                    sim = this
                };

                Crust crust = new Crust(){
                    plate = null,
                    pos = new Vector2I(x,y)
                };
                segment.AddCrust(crust);
                segment.topCrust = crust;

                crusts[(worldSize.Y * x) + y] = crust;
                tiles[(worldSize.Y * x) + y] = segment;
                //GD.Print(segment.crusts.Count);
            }
        }
        CreatePlates(plateCount);
        InitElevation();
    }

    void InitElevation(){
        float[,] falloff = Falloff.GenerateFalloffMap(worldSize.X, worldSize.Y);
        float scale = world.mapScale;
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetSeed(seed);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(10);
        

        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                float elevation = Mathf.InverseLerp(-1, 1, noise.GetNoise((float)x / scale, (float)y / scale)) - falloff[x,y];
                Crust crust = null;

                if (advancedGeneration){
                    TectonicTile segment = GetTectonicTile(x,y);
                    crust = segment.crusts[0];
                } else {
                   crust = GetCrust(x, y);
                }
                
                if (elevation <= seaLevel + 0.05){
                    crust.crustType = CrustTypes.OCEANIC;
                    elevation = Mathf.Lerp(seaLevel - oceanDepth, seaLevel, 1f - Falloff.Evaluate(Mathf.InverseLerp(seaLevel, 0, elevation), 0.15f, 3f));
                } else {
                    crust.crustType = CrustTypes.CONTINENTAL;
                    elevation = Mathf.Lerp(seaLevel, 0.8f, (elevation - seaLevel)/(1f - seaLevel));
                }            
                crust.elevation = elevation; 
            }
        }
             
    }

    #region Advanced Generation

    public void SimStep(){
        // Moves Crust
        MoveAllCrust();
        CheckCollisions();
        ColorDisplay();
    }

    void MoveAllCrust(){
        foreach (TectonicTile tile in tiles){
            tile.lastPlate = tile.topCrust.plate; 
            Vector2I pos = tile.pos;

            foreach (Crust crust in tile.crusts.ToArray()){
                //GD.Print(crust);
                crust.age++;
                if (crust.canMove){
                    crust.canMove = false;

                    crust.moveStep += crust.plate.vel;
                    int mx = (int)Math.Truncate(crust.moveStep.X);
                    if (mx != 0){
                        crust.moveStep.X %= 1;
                    }
                    
                    int my = (int)Math.Truncate(crust.moveStep.Y);
                    if (my != 0){
                        crust.moveStep.Y %= 1;
                    }

                    tile.MoveCrust(crust, crust.pos.X + mx, crust.pos.Y + my);     
                }

            }
        }
        foreach (TectonicTile tile in tiles){
            foreach (Crust crust in tile.crusts){
                crust.canMove = true;
            }
        }
    }

    void CheckCollisions(){
        foreach (TectonicTile tile in tiles){
            if (tile.crusts.Count < 1){
                Crust newCrust = new Crust(){
                    elevation = seaLevel - oceanDepth,
                    crustType = CrustTypes.OCEANIC
                };
                tile.AddCrust(newCrust);
                tile.lastPlate.AddCrust(newCrust);
            }
            tile.topCrust = tile.getTopCrust();

            foreach (Crust crust in tile.crusts.ToArray()){
                if (crust != tile.topCrust){
                    // Oceanic subduction
                    if (crust.crustType == CrustTypes.OCEANIC){
                        crust.pressure += NextFloatInRange(0.1f, 0.2f);

                        if (crust.pressure >= crust.elevation){
                            // Volcanoes
                            if (tile.topCrust.crustType == CrustTypes.OCEANIC){
                                tile.topCrust.elevation += NextFloatInRange(0.01f, 0.04f);
                            } else {
                                tile.topCrust.elevation += NextFloatInRange(0.005f, 0.01f);
                            }
                            
                            DeleteCrust(tile, crust);
                        }                                
                    } else {
                        // Continental Collision
                        // if (!crust.plate.velChanged){
                        //     crust.plate.velChanged = true;
                        //     crust.plate.vel = Vector2.Lerp(crust.plate.vel, topCrust.plate.dir, NextFloatInRange(0.1f, 0.5f));
                        // }
                        
                        tile.topCrust.elevation += NextFloatInRange(0.005f, 0.025f);
                        crust.pressure += NextFloatInRange(0.01f, 0.02f);

                        if (crust.pressure >= crust.elevation){
                            DeleteCrust(tile, crust);
                        }
                    }
                }
            }            
        }
    }

    void DeleteCrust(TectonicTile tile, Crust crust){
        tile.RemoveCrust(crust);
        crust.plate.RemoveCrust(crust);
    }

    #endregion

    #region Simple Generation
    void GetPressure(Crust crust){
        int boundarySize = 3;
        for (int dx = -boundarySize; dx < boundarySize + 1; dx++){
            for (int dy = -boundarySize; dy < boundarySize + 1; dy++){
                Vector2I testPos = GetNewPos(crust.pos, new Vector2I(dx, dy));
                Crust testCrust = GetCrust(testPos.X, testPos.Y);
                if (testCrust.plate != crust.plate){
                    Vector2 relativeVel = crust.plate.vel - testCrust.plate.vel;
                    if (relativeVel.Length() * relativeVel.Normalized().Dot(testCrust.pos - crust.pos) < 0){
                        crust.pressure += 1f * relativeVel.Length();
                    } else {
                        crust.pressure -= 1f * relativeVel.Length();
                    }
                }
            }
        }
        if (crust.pressure > maxPressure){
            maxPressure = crust.pressure;
        }
        else if (crust.pressure < minPressure){
            minPressure = crust.pressure;
        }
    }

    void GrowRange(Crust crust){
        if (crust.pressure != 0){
            
        }
        crust.pressure = Mathf.InverseLerp(minPressure, maxPressure, crust.pressure) * 2 - 1;
        if (crust.pressure > 0){
            // Convergence
            if (crust.crustType == CrustTypes.OCEANIC){
                // Island Chains
                crust.elevation += NextFloatInRange(0.6f, 0.65f) * Mathf.Abs(crust.pressure);
            } else {
                // Mountains
                crust.elevation += NextFloatInRange(0.34f, 0.35f) * Mathf.Abs(crust.pressure);
            }
        } else if (crust.pressure < 0){
            crust.elevation -= NextFloatInRange(0.11f, 0.13f) * Mathf.Abs(crust.pressure);
        }
    }

    # endregion
    float NextFloatInRange(float min, float max){
        return Mathf.Lerp(min, max, rng.NextSingle());
    }

    void ColorDisplay(){
        GD.Print(worldSize);
        if (textureDisplay != null){
            
            for (int x = 0; x < worldSize.X; x++){
                for (int y = 0; y < worldSize.Y; y++){
                    Color color = new Color(0,0,0);

                    Crust crust = null;
                    TectonicTile tile = GetTectonicTile(x,y);
                    if (tile.crusts.Count > 0){
                        crust = tile.topCrust;
                    }

                    if (crust != null){
                        if (crust.elevation < seaLevel){
                            Color deepColor = new Color(0, 0, 0.54f);
                            Color shallowColor = new Color(0, 0.75f, 1);
                            float lerped = crust.elevation + seaLevel;
                            color = new Color((deepColor * (1 - lerped)) + (shallowColor * lerped));
                        } else {
                            float lerped = (crust.elevation - seaLevel)/(1f - seaLevel);
                            Color lowColor = new Color(0.18f, 0.54f, 0.34f);
                            Color highColor = new Color(0.82f, 0.7f, 0.55f);
                            color = new Color((lowColor * (1 - lerped)) + (highColor * lerped));
                        }
                        color = crust.plate.color;                    
                    } else {
                        color = new Color((float)tile.pos.X / worldSize.X,(float)tile.pos.Y / worldSize.Y, 0);
                    }
                    image.SetPixel(x,y,color); 
                }
            }

            textureDisplay.Texture = ImageTexture.CreateFromImage(image);
        }
    }

    public void CreatePlates(int amount){
        List<int> pickedDensities = new List<int>();
        plates = new Plate[amount];
        
        Vector2I[] points = new Vector2I[amount];

        for (int i = 0; i < amount; i++){
            Vector2I pos = new Vector2I(rng.Next(0, worldSize.X), rng.Next(0, worldSize.Y));
            while (points.Contains(pos)){
                pos = new Vector2I(rng.Next(0, worldSize.X), rng.Next(0, worldSize.Y));
            }
            points[i] = pos;

            Plate newPlate = new Plate(){
                vel = new Vector2(NextFloatInRange(-1f, 1f), NextFloatInRange(-1f, 1f)),
                color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle())
            };
            newPlate.density = rng.Next(0, amount + 1);
            while (pickedDensities.Contains(newPlate.density)){
                newPlate.density = rng.Next(0, amount + 1);
            }
            pickedDensities.Add(newPlate.density);
            
            
            plates[i] = newPlate;
        }

        int freeTiles = worldSize.X * worldSize.Y;
        int attempts = freeTiles * 8;
        List<Vector2I> fullPositions = new List<Vector2I>();
        for (int i = 0; i < amount; i++){
            freeTiles -= 1;
            Vector2I pos = points[i];
            Crust crust = GetCrust(pos.X, pos.Y);
            crust.plate = plates[i];
            fullPositions.Add(pos);
        }
        
        while (freeTiles > 0 && attempts > 0){
            world.heightMapProgress += 0.5f;
            attempts -= 1;
            Parallel.ForEach(fullPositions.ToArray(), pos =>{

            });
            foreach (Vector2I pos in fullPositions.ToArray()){
                bool border = false;
                Plate plate = GetCrust(pos.X, pos.Y).plate;
                for (int dx = -1; dx < 2; dx++){
                    for (int dy = -1; dy < 2; dy++){
                        if (dx != 0 && dy != 0 || dx == 0 && dy == 0){
                            continue;
                        }
                        Vector2I nPos = GetNewPos(pos, new Vector2I(dx, dy));
                        Crust crust = GetCrust(nPos.X, nPos.Y);

                        if (crust.plate == null){
                            border = true;
                            if (rng.NextDouble() <= 0.5){
                                plate.AddCrust(crust);
                                fullPositions.Add(nPos);
                                freeTiles -= 1;                                
                            }

                        }
                    }        
                }
                if (!border){
                    fullPositions.Remove(pos);
                }                
            }
        }
    }

    internal Vector2I GetNewPos(Vector2I pos, Vector2I dir){
        Vector2I nPos = pos + dir;
        return new Vector2I(Mathf.PosMod(nPos.X ,worldSize.X), Mathf.PosMod(nPos.Y ,worldSize.Y));
    }
    internal Vector2I GetNewPos(Vector2I pos){
        return new Vector2I(Mathf.PosMod(pos.X ,worldSize.X), Mathf.PosMod(pos.Y ,worldSize.Y));
    }

    internal Crust GetCrust(int x, int y){
        Vector2I pos = new Vector2I(x, y);
        pos = GetNewPos(pos);

        int index = (worldSize.Y * pos.X) + pos.Y;
        return crusts[index];
    }
    internal Crust GetCrust(Vector2I pos){
        pos = GetNewPos(pos);

        int index = (worldSize.Y * pos.X) + pos.Y;
        return crusts[index];
    }

    internal TectonicTile GetTectonicTile(int x, int y){
        Vector2I pos = new Vector2I(x, y);
        pos = GetNewPos(pos);

        int index = (worldSize.Y * pos.X) + pos.Y;
        return tiles[index];
    }
    internal TectonicTile GetTectonicTile(Vector2I pos){
        pos = GetNewPos(pos);

        int index = (worldSize.Y * pos.X) + pos.Y;
        return tiles[index];
    }
    // TODO: Reimplement Tectonics in C#
}
internal class Plate{
    public int density;
    public Vector2 vel = new Vector2();
    public Color color;
    public List<Crust> crusts = new List<Crust>();

    public void AddCrust(Crust crust){
        if (crust.plate != null){
            crust.plate.RemoveCrust(crust);
        }
        crust.plate = this;
        crusts.Add(crust);
    }
    public void RemoveCrust(Crust crust){
        crusts.Remove(crust);
    }
}

internal class TectonicTile{
    public Plate lastPlate;
    public Vector2I pos;
    public Crust topCrust = null;
    public List<Crust> crusts = new List<Crust>();
    public Tectonics sim;

    public void MoveCrust(Crust crust, Vector2I npos){
        npos = sim.GetNewPos(npos);

        TectonicTile newTile = sim.GetTectonicTile(npos);
        newTile.AddCrust(crust);
        RemoveCrust(crust);
    }
    public void MoveCrust(Crust crust, int x, int y){
        Vector2I npos = sim.GetNewPos(new Vector2I(x, y));
        TectonicTile newTile = sim.GetTectonicTile(npos);
        newTile.AddCrust(crust);
        RemoveCrust(crust);
    }

    public void AddCrust(Crust crust){
        crust.pos = pos;
        crusts.Add(crust);
    }
    public void RemoveCrust(Crust crust){
        crusts.Remove(crust);
    }

    public Crust getTopCrust(){
        Crust newTop = null;
        int lowDensity = 2000000000;
        foreach (Crust crust in crusts){
            int relDensity = crust.plate.density + crust.age;
            if (crust.crustType == CrustTypes.CONTINENTAL){
                relDensity -= 1000000;
            }
            if (relDensity < lowDensity){
                lowDensity = relDensity;
                newTop = crust;
            }
        }
        return newTop;
    }
}

internal class Crust{
    public bool canMove = true;
    public float elevation = 0;
    public float pressure = 0;
    public int age = 0;
    public Plate plate;
    public Vector2I pos;
    public Vector2 moveStep;
    public CrustTypes crustType = CrustTypes.OCEANIC;
}

enum CrustTypes{
    CONTINENTAL,
    OCEANIC
}
