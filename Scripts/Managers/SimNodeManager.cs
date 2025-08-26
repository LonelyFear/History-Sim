using Godot;
public delegate void SimStartEvent();
public partial class SimNodeManager : Node
{
    public SimManager simManager;
    public static event SimStartEvent simStartEvent;
    public static void InvokeEvent()
    {
        simStartEvent.Invoke();
    }
}