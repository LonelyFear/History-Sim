using Godot;
using System;

public partial class Game : Node2D
{
    public void GenerateWorld(){
        WorldGeneration worldGen = GetNode<WorldGeneration>("World");
        worldGen.GenerateWorld();
    }
}
