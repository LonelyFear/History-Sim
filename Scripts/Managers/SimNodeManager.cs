using Godot;
public delegate void SimStartEvent();
public partial class SimNodeManager : Node
{
    public SimManager simManager;
    public event SimStartEvent simStartEvent;
    public void InvokeEvent()
    {
        simStartEvent.Invoke();
    }
}