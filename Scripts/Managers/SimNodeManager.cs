using Godot;

public partial class SimNodeManager : Node
{
    public SimManager simManager = new SimManager();
    public override void _Ready()
    {
        simManager.terrainMap = GetNode<Node2D>("/root/Game/Terrain Map");
        simManager.timeManager = GetParent().GetNode<TimeManager>("/root/Game/Time Manager");
        simManager.mapManager = (MapManager)GetParent().GetNode<Node>("Map Manager");

        // Connection
        simManager.worldGenerator = LoadingScreen.generator;
        WorldGenerator.worldgenFinishedEvent += simManager.OnWorldgenFinished;
        //Connect("WorldgenFinished", new Callable(this, nameof()));
    }
}