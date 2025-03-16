using System.Threading.Tasks;
using Godot;

public partial class TimeManager : Node
{
    [Export]
    public Node2D world;
    [Signal]
    public delegate void TickEventHandler();
    [Signal]
    public delegate void YearEventHandler();

    [Export]
    public int monthsPerTick;
    public int month;
    public int year;
    public int totalTicks = 0;

    public ulong tickStartTime = 0;
    public double tickDelta = 1;
    SimManager simManager;
    public override void _Ready()
    {
        world = GetParent().GetNode<Node2D>("World");
        simManager = GetParent().GetNode<SimManager>("Simulation");
        // Connection
        world.Connect("worldgenFinished", new Callable(this, nameof(OnWorldgenFinished)));
    }

    public override void _Process(double delta)
    {
        if (totalTicks == 0){
            TickGame();
        }
        if (simManager.task.IsCompleted){
            tickDelta = (double)(Time.GetTicksMsec() - tickStartTime)/1000;
            TickGame();
            if (simManager.mapUpdate){
                simManager.UpdateMap();
            }
        }
    }

    public void OnWorldgenFinished(){
        //TickGame();
    }

    private void TickGame(){
        tickStartTime = Time.GetTicksMsec();
        totalTicks += 1;
        month += 1;
        EmitSignal(SignalName.Tick);
        if (month > 12){
            month = 1;
            year += 1;
            EmitSignal(SignalName.Year);
        }
        
    }
}
