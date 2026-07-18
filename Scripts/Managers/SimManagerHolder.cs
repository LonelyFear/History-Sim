using Godot;
public delegate void SimStartEvent();
public partial class SimManagerHolder : Node
{
    [Export] public SelectionManager selectionManager;
    public SimManager simManager;
    public event SimStartEvent simStartEvent;
    public void InvokeEvent()
    {
        //GD.Print("Yop");
        simStartEvent.Invoke();
    }
}