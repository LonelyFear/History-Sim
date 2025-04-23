using System.Runtime.Serialization;
using System.Threading.Tasks;
using Godot;

public partial class TimeManager : Node
{
    
    [Signal]
    public delegate void TickEventHandler();
    [Signal]
    public delegate void YearEventHandler();

    [Export]
    public int monthsPerTick;
    public int day;
    public int month;
    public int year;
    public int totalTicks = 0;

    public ulong tickStartTime = 0;
    public float tickDelta = 1;
    public WorldGeneration world;
    public SimManager simManager;
    bool worldGenFinished = false;
    Task monthTask;
    Task yearTask;
    bool doYear = false;
    [Export]
    public bool debuggerMode = false;
    public override void _Ready()
    {
        world = GetNode<WorldGeneration>("/root/Game/World");
        simManager = GetNode<SimManager>("/root/Game/Simulation");
        // Connection
        world.Connect("worldgenFinished", new Callable(this, nameof(OnWorldgenFinished)));
    }

    public override void _Process(double delta)
    {
        if (worldGenFinished && (monthTask == null || monthTask.IsCompleted)){
            if (doYear){
                doYear = false;
                //yearTask = Task.Run();
            }
            if (yearTask == null || yearTask.IsCompleted){
                tickDelta = (Time.GetTicksMsec() - tickStartTime)/1000f;
                TickGame();
                if (simManager.mapUpdate){
                    simManager.UpdateMap();
                }                
            }

        }
    }

    public void OnWorldgenFinished(){
        TickGame();
        worldGenFinished = true;
    }

    private void TickGame(){
        tickStartTime = Time.GetTicksMsec();
        totalTicks += 1;
        month += 1;

        if (!debuggerMode){
            monthTask = Task.Run(simManager.SimTick);
        } else {
            simManager.SimTick();
        }
        
        if (month > 12){
            month = 1;
            year += 1;
            if (!debuggerMode){
                doYear = true;
            } else {
                // Insert year tick function here
            }
        }
    }
}
