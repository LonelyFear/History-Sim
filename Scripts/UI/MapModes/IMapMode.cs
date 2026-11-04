using Godot;

public interface IMapMode
{
    public readonly static Color AnarchyColor = new(0.2f, 0.2f, 0.2f);
    public readonly static Color BaseColor = new(0, 0, 0, 0);
    public readonly static float UnselectedDarkness = 0.4f;

    public static SimManager simManager;
    public static SelectionManager selectionManager;

    public string Id { get; }
    public float Opacity { get; }
    public int FocusTabId { get; }
    public Color GetColor(Region region, out ulong borderId, bool isCenter = false);  
    public bool CanSelectRegion(Region region);
    public string GetHoverText(Region region);
}