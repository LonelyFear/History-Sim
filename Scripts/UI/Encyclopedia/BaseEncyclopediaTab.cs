using Godot;

public abstract partial class BaseEncyclopediaTab : Control
{
    public static SimManager simManager;
     
    public static EncyclopediaManager encyclopediaManager;
    public virtual void InitTab() {}
}