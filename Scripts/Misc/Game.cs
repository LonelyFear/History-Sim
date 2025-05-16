using Godot;
using System;

public partial class Game : Node2D
{
    public int seed;
    public void GenerateWorld(){
        WorldGeneration worldGen = GetNode<WorldGeneration>("World");
        worldGen.GenerateWorld();
    }
}
