using Godot;
using System.Collections.Generic;

public partial class Tectonics : Node
{
    Node world;
    [Export] Sprite2D textureDisplay;
    Image image;
    List<Plate> plates = new List<Plate>();
    Vector2I worldSize;
    long seed;

    public override void _Ready()
    {
        world = GetParent();
        worldSize = (Vector2I)world.Get("worldSize");
        seed = (long)world.Get("seed");
        image = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgb8);
        if (textureDisplay != null){
            textureDisplay.Scale = new Vector2(16,61) * (72f/(float)worldSize.X);
        }
        ColorDisplay();
    }

    void ColorDisplay(){
        if (textureDisplay != null){
            textureDisplay.Texture = ImageTexture.CreateFromImage(image);
        }
    }

    public void CreatePlates(int gridSizeX, int gridSizeY){

    }
    // TODO: Reimplement Tectonics in C#
}
public class Plate{
    public Vector2 vel = new Vector2();
}

public class Crust{
    public float elevation = 0;
    public Plate plate;
}
